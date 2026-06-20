import sys
import json

log_path = r"C:\Users\galiu\.gemini\antigravity\brain\8bab8cb1-88a8-4aca-8804-9242e4d8ad78\.system_generated\logs\overview.txt"
targets = [594, 597, 600]

with open(log_path, 'r', encoding='utf-8') as f:
    for line in f:
        try:
            data = json.loads(line)
            if data.get("step_index") in targets:
                idx = data["step_index"]
                # Write each step's CodeContent to a separate file to avoid terminal truncation
                if "tool_calls" in data and len(data["tool_calls"]) > 0:
                    tc = data["tool_calls"][0]
                    if "args" in tc and "CodeContent" in tc["args"]:
                        with open(f"step_{idx}_content.txt", "w", encoding='utf-8') as out:
                            out.write(tc["args"]["CodeContent"])
                    elif "args" in tc and "ReplacementChunks" in tc["args"]:
                         with open(f"step_{idx}_chunks.txt", "w", encoding='utf-8') as out:
                            out.write(tc["args"]["ReplacementChunks"])
        except:
            continue
