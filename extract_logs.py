import sys
import json

log_path = r"C:\Users\galiu\.gemini\antigravity\brain\8bab8cb1-88a8-4aca-8804-9242e4d8ad78\.system_generated\logs\overview.txt"
targets = [594, 597, 600]

with open(log_path, 'r', encoding='utf-8') as f:
    for line in f:
        try:
            data = json.loads(line)
            if data.get("step_index") in targets:
                print(f"STEP {data['step_index']}:")
                print(json.dumps(data, indent=2))
        except:
            continue
