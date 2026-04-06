from __future__ import annotations

import os
from pathlib import Path
from typing import Literal

import uvicorn
from fastapi import FastAPI, HTTPException, Query, Request, status
from fastapi.responses import PlainTextResponse

from .models import IngestRecord
from .store import DatasetNotFoundError, FileLogStore, RunNotFoundError


def default_log_root() -> Path:
    configured_root = os.getenv("LANDMARKSR_LOG_ROOT")
    if configured_root:
        return Path(configured_root).expanduser().resolve()
    return Path(__file__).resolve().parents[2] / "data" / "logs"


def create_app(log_root: Path | None = None) -> FastAPI:
    app = FastAPI(title="LandmarksR Log Server", version="0.1.0")
    app.state.store = FileLogStore(log_root or default_log_root())

    @app.get("/healthz")
    def healthz() -> dict[str, str]:
        return {"status": "ok"}

    @app.post("/api/v1/records", status_code=status.HTTP_202_ACCEPTED)
    def ingest_record(record: IngestRecord, request: Request) -> dict[str, str]:
        request.app.state.store.ingest_record(record)
        return {"status": "ok"}

    @app.get("/api/v1/runs")
    def list_runs(request: Request, subject_id: str | None = Query(default=None)) -> list[dict]:
        return request.app.state.store.list_runs(subject_id=subject_id)

    @app.get("/api/v1/runs/{run_session_id}")
    def get_run(run_session_id: str, request: Request) -> dict:
        try:
            return request.app.state.store.get_run(run_session_id)
        except RunNotFoundError as exc:
            raise HTTPException(status_code=404, detail=f"Run '{run_session_id}' not found") from exc

    @app.get("/api/v1/runs/{run_session_id}/events")
    def get_events(run_session_id: str, request: Request) -> list[dict]:
        try:
            return request.app.state.store.get_events(run_session_id)
        except RunNotFoundError as exc:
            raise HTTPException(status_code=404, detail=f"Run '{run_session_id}' not found") from exc

    @app.get("/api/v1/runs/{run_session_id}/datasets")
    def list_datasets(run_session_id: str, request: Request) -> list[dict]:
        try:
            return request.app.state.store.list_datasets(run_session_id)
        except RunNotFoundError as exc:
            raise HTTPException(status_code=404, detail=f"Run '{run_session_id}' not found") from exc

    @app.get("/api/v1/runs/{run_session_id}/datasets/{dataset_name}")
    def get_dataset(run_session_id: str, dataset_name: str, request: Request) -> list[dict]:
        try:
            return request.app.state.store.get_dataset(run_session_id, dataset_name)
        except RunNotFoundError as exc:
            raise HTTPException(status_code=404, detail=f"Run '{run_session_id}' not found") from exc
        except DatasetNotFoundError as exc:
            raise HTTPException(
                status_code=404,
                detail=f"Dataset '{dataset_name}' not found for run '{run_session_id}'",
            ) from exc

    @app.get("/api/v1/runs/{run_session_id}/download/{stream:path}")
    def download_stream(
        run_session_id: str,
        stream: str,
        request: Request,
        format: Literal["jsonl", "csv", "tsv"] = Query(default="jsonl"),
    ) -> PlainTextResponse:
        try:
            content = request.app.state.store.download_stream(run_session_id, stream, format)
        except RunNotFoundError as exc:
            raise HTTPException(status_code=404, detail=f"Run '{run_session_id}' not found") from exc
        except DatasetNotFoundError as exc:
            raise HTTPException(status_code=404, detail=f"Stream '{stream}' not found") from exc

        media_type = "application/x-ndjson" if format == "jsonl" else "text/plain; charset=utf-8"
        file_name = f"{stream.replace('/', '_')}.{format}"
        return PlainTextResponse(
            content=content,
            media_type=media_type,
            headers={"Content-Disposition": f'attachment; filename="{file_name}"'},
        )

    return app


app = create_app()


def main() -> None:
    host = os.getenv("LANDMARKSR_LOG_SERVER_HOST", "127.0.0.1")
    port = int(os.getenv("LANDMARKSR_LOG_SERVER_PORT", "8000"))
    uvicorn.run("landmarksr_logs.server:app", host=host, port=port, reload=False)


if __name__ == "__main__":
    main()
