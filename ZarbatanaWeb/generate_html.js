const fs = require('fs');
const marked = require('marked');

// Read the markdown file with utf8
const mdPath = 'g:\\UnityEditor\\TimeAura\\Assets\\TIMEAURA_TECHNICAL_ARCHITECTURE.md';
const htmlPath = 'g:\\UnityEditor\\TimeAura\\ZarbatanaWeb\\assets\\TimeAura_Technical_Architecture.html';

const md = fs.readFileSync(mdPath, 'utf8');

// Parse markdown to HTML
const htmlContent = marked.parse(md);

// HTML template wrapper
const template = `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <title>Technical Architecture | TimeAura</title>
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/github-markdown-css/5.2.0/github-markdown.min.css">
    <style>
        body {
            box-sizing: border-box;
            min-width: 200px;
            max-width: 980px;
            margin: 0 auto;
            padding: 45px;
            background-color: #0d1117;
        }
        .markdown-body {
            color: #c9d1d9;
            background-color: #0d1117;
        }
        @media (max-width: 767px) {
            body {
                padding: 15px;
            }
        }
    </style>
</head>
<body class="markdown-body">
    ${htmlContent}
</body>
</html>`;

// Write HTML file
fs.writeFileSync(htmlPath, template, 'utf8');
console.log('HTML generated successfully.');
