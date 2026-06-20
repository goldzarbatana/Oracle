
with open(r'G:\UnityEditor\TimeAura\Assets\Scripts\Features\UI\Nexus\NexusController.cs', 'r', encoding='utf-8') as f:
    content = f.read()

balance = 0
for i, char in enumerate(content):
    if char == '{':
        balance += 1
    elif char == '}':
        balance -= 1
    
    if balance < 0:
        # Find line number
        line_no = content[:i].count('\n') + 1
        print(f"Extra closing brace at line {line_no}")
        balance = 0

if balance > 0:
    print(f"Missing {balance} closing braces at the end of file.")
elif balance == 0:
    print("Braces are balanced.")
