#!/usr/bin/env python3
"""Where the servers spent their time in a load run, from the Prometheus the servers push their metrics to:
the handlers (one per opcode) of each server by the time they took in all, with how many calls, the mean and the p99.

    python3 -I eng/loadtest/handlers.py http://127.0.0.1:19090 [window in seconds, 600] [rows per server, 12]

The window ends now and should be at least as long as the run: the series of a fresh stack begin with the run, so the
increase over the window is the run. A handler runs from the start to the end of its task, so the waits for Redis and the
database are in its time (the metric `handler.duration`); the callback of the world loop is another figure, in the report of
the bot. eng/loadtest-stand.sh prints this table after a run.
"""
import json
import sys
import urllib.parse
import urllib.request


def query(base, expression):
    url = f"{base}/api/v1/query?" + urllib.parse.urlencode({"query": expression})
    with urllib.request.urlopen(url, timeout=20) as response:
        body = json.load(response)
    if body.get("status") != "success":
        raise RuntimeError(body)
    return body["data"]["result"]


def by(result, *labels):
    return {tuple(r["metric"].get(label, "") for label in labels): float(r["value"][1]) for r in result}


def main():
    base = sys.argv[1].rstrip("/")
    window = int(sys.argv[2]) if len(sys.argv) > 2 else 600
    rows = int(sys.argv[3]) if len(sys.argv) > 3 else 12
    inc = f"{window}s"
    total = by(query(base, f"sum by (job, opcode) (increase(handler_duration_seconds_sum[{inc}]))"), "job", "opcode")
    count = by(query(base, f"sum by (job, opcode) (increase(handler_duration_seconds_count[{inc}]))"), "job", "opcode")
    p99 = by(query(base, f"histogram_quantile(0.99, sum by (job, opcode, le) (increase(handler_duration_seconds_bucket[{inc}])))"), "job", "opcode")
    jobs = sorted({job for job, _ in total})
    for job in jobs:
        mine = sorted(((k, v) for k, v in total.items() if k[0] == job and v > 0), key=lambda kv: -kv[1])
        everything = sum(v for _, v in mine)
        print(f"\n{job}: handlers took {everything:.2f} s in all over the window of {inc}")
        print(f"  {'opcode':<46} {'calls':>9} {'total s':>9} {'share':>6} {'mean ms':>8} {'p99 ms':>8}")
        for (_, opcode), seconds in mine[:rows]:
            calls = count.get((job, opcode), 0)
            mean = seconds / calls * 1000 if calls else 0
            tail = p99.get((job, opcode))
            print(f"  {opcode:<46} {calls:>9.0f} {seconds:>9.2f} {seconds / everything:>6.1%} {mean:>8.3f} {('%8.3f' % (tail * 1000)) if tail == tail and tail is not None else '       -'}")


if __name__ == "__main__":
    main()
