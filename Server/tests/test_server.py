from __future__ import annotations

import json

from fastapi.testclient import TestClient

from landmarksr_logs.client import LogApiClient
from landmarksr_logs.server import create_app


def sample_event(subject_id: str = "unassigned_subject") -> dict:
    return {
        "record_type": "event",
        "source": "session",
        "level": "info",
        "event_name": "run_session_started",
        "ts_utc": "2026-04-05T20:00:00.000Z",
        "ts_unix_ms": 1775419200000,
        "run_session_id": "run-123",
        "subject_id": subject_id,
        "task_name": None,
        "dataset_name": None,
        "repeat_index": None,
        "subtask_index": None,
        "trial_id": None,
        "message": "Run session started",
        "payload": {"run_session_id": "run-123"},
    }


def sample_dataset_row(subject_id: str = "S-100", stimulus: str = "A") -> dict:
    return {
        "record_type": "dataset_row",
        "source": "dataset",
        "dataset_name": "nback_text",
        "ts_utc": "2026-04-05T20:00:10.000Z",
        "ts_unix_ms": 1775419210000,
        "run_session_id": "run-123",
        "subject_id": subject_id,
        "row": {
            "trial_id": "1",
            "stimulus": stimulus,
            "selected_response": "match",
        },
    }


def test_healthz(tmp_path):
    app = create_app(tmp_path)
    with TestClient(app) as client:
        response = client.get("/healthz")

    assert response.status_code == 200
    assert response.json() == {"status": "ok"}


def test_ingest_event_creates_manifest_and_event_file(tmp_path):
    app = create_app(tmp_path)
    with TestClient(app) as client:
        response = client.post("/api/v1/records", json=sample_event())

    assert response.status_code == 202
    manifest_path = tmp_path / "run-123" / "manifest.json"
    events_path = tmp_path / "run-123" / "events.jsonl"
    assert manifest_path.exists()
    assert events_path.exists()

    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    assert manifest["run_session_id"] == "run-123"
    assert manifest["event_count"] == 1
    assert manifest["subject_id"] == "unassigned_subject"


def test_subject_id_updates_when_real_subject_arrives(tmp_path):
    app = create_app(tmp_path)
    with TestClient(app) as client:
        client.post("/api/v1/records", json=sample_event())
        client.post("/api/v1/records", json=sample_event(subject_id="S-100"))
        response = client.get("/api/v1/runs", params={"subject_id": "S-100"})

    assert response.status_code == 200
    runs = response.json()
    assert len(runs) == 1
    assert runs[0]["subject_id"] == "S-100"


def test_ingest_dataset_row_tracks_counts_and_columns(tmp_path):
    app = create_app(tmp_path)
    with TestClient(app) as client:
        client.post("/api/v1/records", json=sample_event(subject_id="S-100"))
        response = client.post("/api/v1/records", json=sample_dataset_row())

    assert response.status_code == 202
    manifest = json.loads((tmp_path / "run-123" / "manifest.json").read_text(encoding="utf-8"))
    assert manifest["available_datasets"] == ["nback_text"]
    assert manifest["dataset_row_counts"]["nback_text"] == 1
    assert manifest["dataset_columns"]["nback_text"] == ["trial_id", "stimulus", "selected_response"]


def test_download_endpoints_emit_tabular_exports(tmp_path):
    app = create_app(tmp_path)
    with TestClient(app) as client:
        client.post("/api/v1/records", json=sample_event(subject_id="S-100"))
        client.post("/api/v1/records", json=sample_dataset_row())

        events_csv = client.get("/api/v1/runs/run-123/download/events", params={"format": "csv"})
        dataset_tsv = client.get(
            "/api/v1/runs/run-123/download/datasets/nback_text",
            params={"format": "tsv"},
        )

    assert events_csv.status_code == 200
    assert "event_name" in events_csv.text
    assert "run_session_started" in events_csv.text
    assert dataset_tsv.status_code == 200
    assert "run_session_id\tsubject_id\tts_utc\tts_unix_ms\tdataset_name\ttrial_id\tstimulus\tselected_response" in dataset_tsv.text
    assert "match" in dataset_tsv.text


def test_client_returns_dataframes(tmp_path):
    app = create_app(tmp_path)
    with TestClient(app) as server_client:
        server_client.post("/api/v1/records", json=sample_event(subject_id="S-100"))
        server_client.post("/api/v1/records", json=sample_dataset_row(subject_id="S-100", stimulus="B"))

        client = LogApiClient(http_client=server_client)
        runs = client.list_runs()
        events = client.get_events("run-123")
        datasets = client.list_datasets("run-123")
        frame = client.get_dataset("run-123", "nback_text")

    assert list(runs["run_session_id"]) == ["run-123"]
    assert "payload_json" in events.columns
    assert list(datasets["dataset_name"]) == ["nback_text"]
    assert frame.loc[0, "stimulus"] == "B"
