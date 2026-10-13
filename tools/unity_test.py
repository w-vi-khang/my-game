#!/usr/bin/env python3
"""Chạy test Unity (EditMode mặc định) bằng dòng lệnh, đọc file kết quả XML, trả exit code.

Unity không định nghĩa exit code cho -runTests ("There is currently no common definition for exit codes"
— docs 6000.4 Test Framework, reference-command-line), nên script này đọc file kết quả thay vì tin exit code.

Dùng:  python3 tools/unity_test.py [EditMode|PlayMode]
Biến môi trường: UNITY_PATH (mặc định: Unity 6000.4.8f1 cài qua Hub trên macOS).
Exit 0 = mọi test đạt (kể cả khi chưa có test nào); 1 = có test lỗi hoặc không đọc được kết quả.
"""
import os
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROJECT = os.path.join(ROOT, "client")
STATE = os.path.join(PROJECT, "Logs", "tests")  # trong client/Logs (đã ignore); Unity báo lỗi tên thư mục bắt đầu bằng "."
UNITY = os.environ.get(
    "UNITY_PATH", "/Applications/Unity/Hub/Editor/6000.4.8f1/Unity.app/Contents/MacOS/Unity"
)


def main():
    platform = sys.argv[1] if len(sys.argv) > 1 else "EditMode"
    os.makedirs(STATE, exist_ok=True)
    results = os.path.join(STATE, f"unity-{platform.lower()}.xml")
    log = os.path.join(STATE, f"unity-{platform.lower()}.log")
    if os.path.exists(results):
        os.remove(results)

    if not os.path.exists(UNITY):
        print(f"Không thấy Unity tại {UNITY} — đặt biến UNITY_PATH.")
        return 1

    cmd = [UNITY, "-batchmode", "-projectPath", PROJECT, "-runTests",
           "-testPlatform", platform, "-testResults", results, "-logFile", log]
    proc = subprocess.run(cmd)

    if not os.path.exists(results):
        print(f"Unity không tạo file kết quả (exit {proc.returncode}). "
              f"Unity có đang mở project không? Xem log: {log}")
        return 1

    run = ET.parse(results).getroot()  # NUnit 3: <test-run result=... total=... passed=... failed=...>
    total = int(run.get("total", 0))
    passed = int(run.get("passed", 0))
    failed = int(run.get("failed", 0))
    skipped = int(run.get("skipped", 0))
    print(f"{platform}: {total} test — đạt {passed}, lỗi {failed}, bỏ qua {skipped} "
          f"(kết quả: {run.get('result')}) · {results}")
    for case in run.iter("test-case"):
        if case.get("result") == "Failed":
            msg = case.findtext("failure/message", "").strip().splitlines()
            print(f"  ✗ {case.get('fullname')}: {msg[0] if msg else ''}")
    return 0 if failed == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
