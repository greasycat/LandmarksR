from __future__ import annotations

import argparse
import json
import os
import sys
from pathlib import Path
from typing import Any

import httpx
import pandas as pd

from .tabular import (
    EVENT_COLUMNS,
    build_dataset_columns,
    flatten_dataset_record,
    flatten_event_record,
    infer_dataset_columns,
)


class LogApiClient:
    def __init__(
        self,
        base_url: str = "http://127.0.0.1:8000",
        timeout: float = 30.0,
        http_client: Any | None = None,
    ):
        self._owns_client = http_client is None
        self._client = http_client or httpx.Client(base_url=base_url.rstrip("/"), timeout=timeout)

    def close(self) -> None:
        if self._owns_client and hasattr(self._client, "close"):
            self._client.close()

    def __enter__(self) -> "LogApiClient":
        return self

    def __exit__(self, exc_type, exc, tb) -> None:
        self.close()

    def list_runs(self, subject_id: str | None = None) -> pd.DataFrame:
        params = {"subject_id": subject_id} if subject_id else None
        items = self._get_json("/api/v1/runs", params=params)
        return pd.DataFrame(items)

    def get_run(self, run_session_id: str) -> dict[str, Any]:
        return self._get_json(f"/api/v1/runs/{run_session_id}")

    def get_events(self, run_session_id: str) -> pd.DataFrame:
        items = self._get_json(f"/api/v1/runs/{run_session_id}/events")
        rows = [flatten_event_record(item) for item in items]
        return pd.DataFrame(rows, columns=EVENT_COLUMNS)

    def list_datasets(self, run_session_id: str) -> pd.DataFrame:
        items = self._get_json(f"/api/v1/runs/{run_session_id}/datasets")
        return pd.DataFrame(items)

    def get_dataset(self, run_session_id: str, dataset_name: str) -> pd.DataFrame:
        items = self._get_json(f"/api/v1/runs/{run_session_id}/datasets/{dataset_name}")
        manifest = self.get_run(run_session_id)
        dataset_columns = manifest.get("dataset_columns", {}).get(dataset_name) or infer_dataset_columns(items)
        ordered_columns = build_dataset_columns(dataset_columns)
        rows = [flatten_dataset_record(item, dataset_columns) for item in items]
        return pd.DataFrame(rows, columns=ordered_columns)

    def download_stream(self, run_session_id: str, stream: str, format: str = "jsonl") -> str:
        response = self._client.get(
            f"/api/v1/runs/{run_session_id}/download/{stream}",
            params={"format": format},
        )
        response.raise_for_status()
        return response.text

    def _get_json(self, path: str, params: dict[str, Any] | None = None) -> Any:
        response = self._client.get(path, params=params)
        response.raise_for_status()
        return response.json()


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Fetch experiment logs from the LandmarksR log server.")
    parser.add_argument(
        "--base-url",
        default=os.getenv("LANDMARKSR_LOG_SERVER_URL", "http://127.0.0.1:8000"),
        help="Base URL for the FastAPI log server.",
    )

    subparsers = parser.add_subparsers(dest="command", required=True)

    list_runs_parser = subparsers.add_parser("list-runs", help="List available runs.")
    list_runs_parser.add_argument("--subject-id")
    list_runs_parser.add_argument("--format", choices=["csv", "tsv", "json"], default="csv")
    list_runs_parser.add_argument("--output")

    get_run_parser = subparsers.add_parser("get-run", help="Fetch a single run manifest.")
    get_run_parser.add_argument("run_session_id")
    get_run_parser.add_argument("--output")

    events_parser = subparsers.add_parser("get-events", help="Fetch events for a run.")
    events_parser.add_argument("run_session_id")
    events_parser.add_argument("--format", choices=["csv", "tsv", "json"], default="csv")
    events_parser.add_argument("--output")

    list_datasets_parser = subparsers.add_parser("list-datasets", help="List datasets for a run.")
    list_datasets_parser.add_argument("run_session_id")
    list_datasets_parser.add_argument("--format", choices=["csv", "tsv", "json"], default="csv")
    list_datasets_parser.add_argument("--output")

    dataset_parser = subparsers.add_parser("get-dataset", help="Fetch one dataset for a run.")
    dataset_parser.add_argument("run_session_id")
    dataset_parser.add_argument("dataset_name")
    dataset_parser.add_argument("--format", choices=["csv", "tsv", "json"], default="csv")
    dataset_parser.add_argument("--output")

    download_parser = subparsers.add_parser("download", help="Download a raw stream export from the server.")
    download_parser.add_argument("run_session_id")
    download_parser.add_argument("stream")
    download_parser.add_argument("--format", choices=["jsonl", "csv", "tsv"], default="jsonl")
    download_parser.add_argument("--output")

    args = parser.parse_args(argv)

    with LogApiClient(base_url=args.base_url) as client:
        if args.command == "list-runs":
            frame = client.list_runs(subject_id=args.subject_id)
            _emit_dataframe(frame, args.format, args.output)
            return

        if args.command == "get-run":
            payload = client.get_run(args.run_session_id)
            _emit_json(payload, args.output)
            return

        if args.command == "get-events":
            frame = client.get_events(args.run_session_id)
            _emit_dataframe(frame, args.format, args.output)
            return

        if args.command == "list-datasets":
            frame = client.list_datasets(args.run_session_id)
            _emit_dataframe(frame, args.format, args.output)
            return

        if args.command == "get-dataset":
            frame = client.get_dataset(args.run_session_id, args.dataset_name)
            _emit_dataframe(frame, args.format, args.output)
            return

        if args.command == "download":
            text = client.download_stream(args.run_session_id, args.stream, args.format)
            _emit_text(text, args.output)
            return

    raise SystemExit(1)


def _emit_dataframe(frame: pd.DataFrame, format_name: str, output_path: str | None) -> None:
    if format_name == "json":
        payload = frame.to_dict(orient="records")
        _emit_json(payload, output_path)
        return

    separator = "\t" if format_name == "tsv" else ","
    text = frame.to_csv(index=False, sep=separator, lineterminator="\n")
    _emit_text(text, output_path)


def _emit_json(payload: Any, output_path: str | None) -> None:
    text = json.dumps(payload, indent=2, ensure_ascii=False) + "\n"
    _emit_text(text, output_path)


def _emit_text(text: str, output_path: str | None) -> None:
    if output_path:
        path = Path(output_path)
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")
        return

    sys.stdout.write(text)


if __name__ == "__main__":
    main()
