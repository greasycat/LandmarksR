from __future__ import annotations

import json
import threading
from collections.abc import Mapping
from datetime import UTC, datetime
from pathlib import Path
from typing import Any

from .models import DEFAULT_SUBJECT_ID, DatasetLogRecord, EventLogRecord, RunManifest
from .tabular import (
    EVENT_COLUMNS,
    build_dataset_columns,
    flatten_dataset_record,
    flatten_event_record,
    infer_dataset_columns,
    to_delimited_text,
)


class RunNotFoundError(FileNotFoundError):
    pass


class DatasetNotFoundError(FileNotFoundError):
    pass


class FileLogStore:
    def __init__(self, log_root: Path):
        self.log_root = Path(log_root)
        self.log_root.mkdir(parents=True, exist_ok=True)
        self._write_lock = threading.Lock()

    def ingest_record(self, record: EventLogRecord | DatasetLogRecord) -> RunManifest:
        run_session_id = record.run_session_id.strip()
        if not run_session_id:
            raise ValueError("run_session_id is required")

        with self._write_lock:
            run_dir = self._run_dir(run_session_id)
            run_dir.mkdir(parents=True, exist_ok=True)
            (run_dir / "datasets").mkdir(parents=True, exist_ok=True)

            manifest = self._load_manifest(run_session_id) or self._new_manifest(record)
            manifest.updated_at = record.ts_utc or manifest.updated_at
            manifest.subject_id = self._merge_subject_id(manifest.subject_id, record.subject_id)

            serialized_record = self._serialize_record(record)
            if isinstance(record, EventLogRecord):
                self._append_jsonl(self._events_path(run_session_id), serialized_record)
                manifest.event_count += 1
            else:
                dataset_name = record.dataset_name.strip()
                if not dataset_name:
                    raise ValueError("dataset_name is required for dataset rows")

                self._append_jsonl(self._dataset_path(run_session_id, dataset_name), serialized_record)
                if dataset_name not in manifest.available_datasets:
                    manifest.available_datasets.append(dataset_name)
                manifest.dataset_row_counts[dataset_name] = manifest.dataset_row_counts.get(dataset_name, 0) + 1
                current_columns = manifest.dataset_columns.setdefault(dataset_name, [])
                for column in record.row.keys():
                    if column not in current_columns:
                        current_columns.append(column)

            self._write_manifest(manifest)
            return manifest

    def list_runs(self, subject_id: str | None = None) -> list[dict[str, Any]]:
        runs: list[dict[str, Any]] = []
        requested_subject_id = (subject_id or "").strip()
        for manifest_path in sorted(self.log_root.glob("*/manifest.json")):
            manifest = RunManifest.model_validate_json(manifest_path.read_text(encoding="utf-8"))
            if requested_subject_id and manifest.subject_id != requested_subject_id:
                continue
            runs.append(manifest.model_dump(mode="json"))

        runs.sort(key=lambda item: item["updated_at"], reverse=True)
        return runs

    def get_run(self, run_session_id: str) -> dict[str, Any]:
        manifest = self._require_manifest(run_session_id)
        return manifest.model_dump(mode="json")

    def list_datasets(self, run_session_id: str) -> list[dict[str, Any]]:
        manifest = self._require_manifest(run_session_id)
        return [
            {
                "dataset_name": dataset_name,
                "row_count": manifest.dataset_row_counts.get(dataset_name, 0),
                "columns": manifest.dataset_columns.get(dataset_name, []),
            }
            for dataset_name in manifest.available_datasets
        ]

    def get_events(self, run_session_id: str) -> list[dict[str, Any]]:
        self._require_manifest(run_session_id)
        return self._read_jsonl(self._events_path(run_session_id))

    def get_dataset(self, run_session_id: str, dataset_name: str) -> list[dict[str, Any]]:
        manifest = self._require_manifest(run_session_id)
        if dataset_name not in manifest.available_datasets:
            raise DatasetNotFoundError(dataset_name)
        return self._read_jsonl(self._dataset_path(run_session_id, dataset_name))

    def download_stream(self, run_session_id: str, stream: str, format_name: str) -> str:
        normalized_format = format_name.lower()
        if normalized_format not in {"jsonl", "csv", "tsv"}:
            raise ValueError(f"Unsupported format '{format_name}'")

        if stream == "events":
            records = self.get_events(run_session_id)
            if normalized_format == "jsonl":
                return self._jsonl_text(self._events_path(run_session_id))

            rows = [flatten_event_record(record) for record in records]
            delimiter = "\t" if normalized_format == "tsv" else ","
            return to_delimited_text(rows, EVENT_COLUMNS, delimiter)

        dataset_prefix = "datasets/"
        if not stream.startswith(dataset_prefix):
            raise DatasetNotFoundError(stream)

        dataset_name = stream[len(dataset_prefix) :]
        records = self.get_dataset(run_session_id, dataset_name)
        if normalized_format == "jsonl":
            return self._jsonl_text(self._dataset_path(run_session_id, dataset_name))

        manifest = self._require_manifest(run_session_id)
        dataset_columns = manifest.dataset_columns.get(dataset_name) or infer_dataset_columns(records)
        ordered_columns = build_dataset_columns(dataset_columns)
        rows = [flatten_dataset_record(record, dataset_columns) for record in records]
        delimiter = "\t" if normalized_format == "tsv" else ","
        return to_delimited_text(rows, ordered_columns, delimiter)

    def _load_manifest(self, run_session_id: str) -> RunManifest | None:
        manifest_path = self._manifest_path(run_session_id)
        if not manifest_path.exists():
            return None
        return RunManifest.model_validate_json(manifest_path.read_text(encoding="utf-8"))

    def _require_manifest(self, run_session_id: str) -> RunManifest:
        manifest = self._load_manifest(run_session_id)
        if manifest is None:
            raise RunNotFoundError(run_session_id)
        return manifest

    def _new_manifest(self, record: EventLogRecord | DatasetLogRecord) -> RunManifest:
        created_at = record.ts_utc or datetime.now(UTC).isoformat()
        return RunManifest(
            run_session_id=record.run_session_id,
            subject_id=self._merge_subject_id("", record.subject_id),
            created_at=created_at,
            updated_at=created_at,
        )

    def _write_manifest(self, manifest: RunManifest) -> None:
        manifest_path = self._manifest_path(manifest.run_session_id)
        manifest_path.parent.mkdir(parents=True, exist_ok=True)
        temp_path = manifest_path.with_suffix(".tmp")
        temp_path.write_text(
            json.dumps(manifest.model_dump(mode="json"), indent=2, ensure_ascii=False) + "\n",
            encoding="utf-8",
        )
        temp_path.replace(manifest_path)

    @staticmethod
    def _serialize_record(record: EventLogRecord | DatasetLogRecord) -> str:
        return json.dumps(record.model_dump(mode="json"), separators=(",", ":"), ensure_ascii=False)

    @staticmethod
    def _append_jsonl(path: Path, line: str) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        with path.open("a", encoding="utf-8") as handle:
            handle.write(line)
            handle.write("\n")

    @staticmethod
    def _read_jsonl(path: Path) -> list[dict[str, Any]]:
        if not path.exists():
            return []

        items: list[dict[str, Any]] = []
        with path.open("r", encoding="utf-8") as handle:
            for line in handle:
                if not line.strip():
                    continue
                value = json.loads(line)
                if isinstance(value, Mapping):
                    items.append(dict(value))
        return items

    @staticmethod
    def _jsonl_text(path: Path) -> str:
        if not path.exists():
            return ""
        return path.read_text(encoding="utf-8")

    @staticmethod
    def _merge_subject_id(current_subject_id: str, incoming_subject_id: str) -> str:
        normalized_incoming = (incoming_subject_id or "").strip()
        normalized_current = (current_subject_id or "").strip()
        if normalized_incoming and normalized_incoming != DEFAULT_SUBJECT_ID:
            return normalized_incoming
        if normalized_current:
            return normalized_current
        return normalized_incoming or DEFAULT_SUBJECT_ID

    def _run_dir(self, run_session_id: str) -> Path:
        return self.log_root / run_session_id

    def _manifest_path(self, run_session_id: str) -> Path:
        return self._run_dir(run_session_id) / "manifest.json"

    def _events_path(self, run_session_id: str) -> Path:
        return self._run_dir(run_session_id) / "events.jsonl"

    def _dataset_path(self, run_session_id: str, dataset_name: str) -> Path:
        return self._run_dir(run_session_id) / "datasets" / f"{dataset_name}.jsonl"
