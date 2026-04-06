from __future__ import annotations

import csv
import io
import json
from collections.abc import Iterable, Mapping, Sequence
from typing import Any

EVENT_COLUMNS = [
    "ts_utc",
    "ts_unix_ms",
    "level",
    "source",
    "event_name",
    "run_session_id",
    "subject_id",
    "task_name",
    "dataset_name",
    "repeat_index",
    "subtask_index",
    "trial_id",
    "message",
    "payload_json",
]

DATASET_ENVELOPE_COLUMNS = [
    "run_session_id",
    "subject_id",
    "ts_utc",
    "ts_unix_ms",
    "dataset_name",
]


def compact_json(value: Mapping[str, Any] | None) -> str:
    if not value:
        return ""
    return json.dumps(value, separators=(",", ":"), ensure_ascii=False, sort_keys=True)


def stringify(value: Any) -> str:
    if value is None:
        return ""
    return str(value)


def build_dataset_columns(dataset_columns: Iterable[str]) -> list[str]:
    columns = list(DATASET_ENVELOPE_COLUMNS)
    for column in dataset_columns:
        if not column or column in columns:
            continue
        columns.append(column)
    return columns


def infer_dataset_columns(records: Sequence[Mapping[str, Any]]) -> list[str]:
    discovered: list[str] = []
    for record in records:
        row = record.get("row", {})
        if not isinstance(row, Mapping):
            continue
        for column in row.keys():
            if column not in discovered:
                discovered.append(column)
    return discovered


def flatten_event_record(record: Mapping[str, Any]) -> dict[str, str]:
    payload = record.get("payload")
    payload_mapping = payload if isinstance(payload, Mapping) else None
    return {
        "ts_utc": stringify(record.get("ts_utc")),
        "ts_unix_ms": stringify(record.get("ts_unix_ms")),
        "level": stringify(record.get("level")),
        "source": stringify(record.get("source")),
        "event_name": stringify(record.get("event_name")),
        "run_session_id": stringify(record.get("run_session_id")),
        "subject_id": stringify(record.get("subject_id")),
        "task_name": stringify(record.get("task_name")),
        "dataset_name": stringify(record.get("dataset_name")),
        "repeat_index": stringify(record.get("repeat_index")),
        "subtask_index": stringify(record.get("subtask_index")),
        "trial_id": stringify(record.get("trial_id")),
        "message": stringify(record.get("message")),
        "payload_json": compact_json(payload_mapping),
    }


def flatten_dataset_record(
    record: Mapping[str, Any],
    dataset_columns: Iterable[str] | None = None,
) -> dict[str, str]:
    row = record.get("row", {})
    row_mapping = row if isinstance(row, Mapping) else {}
    ordered_columns = list(dataset_columns or row_mapping.keys())

    flattened = {
        "run_session_id": stringify(record.get("run_session_id")),
        "subject_id": stringify(record.get("subject_id")),
        "ts_utc": stringify(record.get("ts_utc")),
        "ts_unix_ms": stringify(record.get("ts_unix_ms")),
        "dataset_name": stringify(record.get("dataset_name")),
    }

    for column in ordered_columns:
        if column in flattened:
            continue
        flattened[column] = stringify(row_mapping.get(column, ""))

    return flattened


def to_delimited_text(
    rows: Sequence[Mapping[str, str]],
    columns: Sequence[str],
    delimiter: str,
) -> str:
    buffer = io.StringIO(newline="")
    writer = csv.DictWriter(
        buffer,
        fieldnames=list(columns),
        delimiter=delimiter,
        lineterminator="\n",
        extrasaction="ignore",
    )
    writer.writeheader()
    for row in rows:
        writer.writerow({column: stringify(row.get(column, "")) for column in columns})
    return buffer.getvalue()
