"use client";

import { useEffect, useRef, useState } from "react";

type StandardTimeFieldProps = {
  name: string;
  value: string;
  onChange: (value: string) => void;
  label: string;
  intervalMinutes?: 15 | 30;
};

const pad = (value: number) => String(value).padStart(2, "0");

function parts(value: string) {
  const [hours = "00", minutes = "00"] = value.split(":");
  const hour = Math.max(0, Math.min(23, Number(hours) || 0));
  const minute = Math.max(0, Math.min(59, Number(minutes) || 0));
  return { hour, minute };
}

function readableTime(value: string) {
  const { hour, minute } = parts(value);
  return new Intl.DateTimeFormat("en-IN", {
    hour: "numeric",
    minute: "2-digit",
  }).format(new Date(2000, 0, 1, hour, minute));
}

export function StandardTimeField({
  name,
  value,
  onChange,
  label,
  intervalMinutes = 15,
}: StandardTimeFieldProps) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const { hour, minute } = parts(value);
  const isPm = hour >= 12;
  const currentHour = hour % 12 || 12;
  const minuteOptions = Array.from(
    { length: 60 / intervalMinutes },
    (_, index) => index * intervalMinutes,
  );

  useEffect(() => {
    const close = (event: MouseEvent) => {
      if (ref.current && !ref.current.contains(event.target as Node))
        setOpen(false);
    };
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, []);

  function setTime(nextHour: number, nextMinute: number, nextIsPm: boolean) {
    let hour24 = nextHour % 12;
    if (nextIsPm) hour24 += 12;
    onChange(`${pad(hour24)}:${pad(nextMinute)}`);
  }

  return (
    <div className="standard-time-field" ref={ref}>
      <span className="standard-time-label">{label}</span>
      <input type="hidden" name={name} value={value} />
      <button
        type="button"
        className="standard-time-trigger"
        aria-expanded={open}
        aria-haspopup="dialog"
        onClick={() => setOpen((current) => !current)}
      >
        <span>{readableTime(value)}</span>
        <i aria-hidden="true">◷</i>
      </button>
      {open && (
        <section
          className="standard-time-popover"
          role="dialog"
          aria-label={`${label} picker`}
        >
          <div>
            <span>Hour</span>
            <div className="standard-time-options">
              {[12, ...Array.from({ length: 11 }, (_, index) => index + 1)].map(
                (option) => (
                  <button
                    key={option}
                    type="button"
                    aria-pressed={currentHour === option}
                    onClick={() => setTime(option, minute, isPm)}
                  >
                    {pad(option)}
                  </button>
                ),
              )}
            </div>
          </div>
          <div>
            <span>Minute</span>
            <div className="standard-time-options">
              {minuteOptions.map((option) => (
                <button
                  key={option}
                  type="button"
                  aria-pressed={minute === option}
                  onClick={() => setTime(currentHour, option, isPm)}
                >
                  {pad(option)}
                </button>
              ))}
            </div>
          </div>
          <div>
            <span>Period</span>
            <div className="standard-time-options standard-time-periods">
              {[
                ["AM", false],
                ["PM", true],
              ].map(([period, periodIsPm]) => (
                <button
                  key={String(period)}
                  type="button"
                  aria-pressed={isPm === periodIsPm}
                  onClick={() =>
                    setTime(currentHour, minute, Boolean(periodIsPm))
                  }
                >
                  {period}
                </button>
              ))}
            </div>
          </div>
        </section>
      )}
    </div>
  );
}
