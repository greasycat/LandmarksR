from __future__ import annotations

from typing import Annotated, Any, Literal

from pydantic import BaseModel, ConfigDict, Field

DEFAULT_SUBJECT_ID = "unassigned_subject"


class EventLogRecord(BaseModel):
    model_config = ConfigDict(extra="forbid")

    record_type: Literal["event"] = "event"
    source: str = ""
    level: str = ""
    event_name: str = ""
    ts_utc: str
    ts_unix_ms: int
    run_session_id: str
    subject_id: str = DEFAULT_SUBJECT_ID
    task_name: str | None = None
    dataset_name: str | None = None
    repeat_index: int | None = None
    subtask_index: int | None = None
    trial_id: str | None = None
    message: str = ""
    payload: dict[str, Any] = Field(default_factory=dict)


class DatasetLogRecord(BaseModel):
    model_config = ConfigDict(extra="forbid")

    record_type: Literal["dataset_row"] = "dataset_row"
    source: str = "dataset"
    dataset_name: str
    ts_utc: str
    ts_unix_ms: int
    run_session_id: str
    subject_id: str = DEFAULT_SUBJECT_ID
    row: dict[str, str] = Field(default_factory=dict)


IngestRecord = Annotated[EventLogRecord | DatasetLogRecord, Field(discriminator="record_type")]


class RunManifest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    run_session_id: str
    subject_id: str = DEFAULT_SUBJECT_ID
    created_at: str
    updated_at: str
    available_datasets: list[str] = Field(default_factory=list)
    event_count: int = 0
    dataset_row_counts: dict[str, int] = Field(default_factory=dict)
    dataset_columns: dict[str, list[str]] = Field(default_factory=dict)
