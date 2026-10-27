"""Build a navigable UI index from locally extracted XAML. Python stdlib only."""
from pathlib import Path
import collections
import hashlib
import json
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'analysis' / 'shiguang'
X = '{http://schemas.microsoft.com/winfx/2006/xaml}'
inventory = json.loads((OUT / 'inventory.json').read_text(encoding='utf-8-sig'))
resources = {r['name'].removeprefix('\\Resources\\').replace('\\', '/'): r['value']
             for r in inventory if r['name'].startswith('\\Resources\\') and 'value' in r}
pages = []
for path in sorted((OUT / 'xaml').rglob('*.xaml')):
    if not path.is_file():
        continue
    root = ET.parse(path).getroot()
    nodes = []
    def visit(element, parent=None):
        index = len(nodes)
        attrs = {key.replace(X, 'x:'): value for key, value in element.attrib.items()}
        uid = element.get(X + 'Uid')
        values = {key[len(uid)+1:]: value for key, value in resources.items()
                  if uid and key.startswith(uid + '/')}
        nodes.append(dict(index=index, parent=parent, type=element.tag.split('}')[-1],
                          attributes=attrs, resource_values=values))
        for child in element:
            visit(child, index)
    visit(root)
    pages.append(dict(file=path.relative_to(OUT).as_posix(), nodes=nodes))
(OUT / 'ui-map.json').write_text(json.dumps(pages, ensure_ascii=False, indent=2), encoding='utf-8')
(OUT / 'strings.json').write_text(json.dumps(resources, ensure_ascii=False, indent=2), encoding='utf-8')

lines = ['# 拾光壁纸 UI 资源索引', '',
         '以下是编译资源的解析结果，不等同于原始源码；层级与常量可供复现参考，运行时变化需要另外验证。', '',
         f'- 资源候选：{len(inventory)}', f'- 成功解析的 XAML 文件：{len(pages)}',
         f'- 文本资源：{len(resources)}', '',
         '原始布局位于 `xaml/`；`ui-map.json` 保留节点父子关系、属性及 x:Uid 对应文本。', '',
         '## 页面清单', '', '| 文件 | XML 节点数 |', '| --- | ---: |']
for page in pages:
    lines.append(f"| [{page['file']}]({page['file']}) | {len(page['nodes'])} |")
for page in pages:
    if not page['file'].startswith('xaml/Files/Pages/'):
        continue
    lines += ['', f"## {Path(page['file']).stem} 命名控件", '', '| 类型 | 名称 | 资源文本 | 布局属性 |', '| --- | --- | --- | --- |']
    for node in page['nodes']:
        attrs = node['attributes']
        name = attrs.get('x:Name') or attrs.get('x:Uid')
        if not name:
            continue
        values = '; '.join(f'{k}: {v}' for k, v in node['resource_values'].items())
        layout = '; '.join(f'{k}={v}' for k, v in attrs.items()
                           if k in {'Width', 'Height', 'MaxWidth', 'Margin', 'Padding', 'CornerRadius', 'Visibility', 'Style', 'Background', 'Orientation', 'DisplayMode', 'OpenPaneLength'})
        def clean(value):
            return str(value).replace('|', '\\|').replace('\n', '<br>')
        lines.append('| ' + ' | '.join(map(clean, [node['type'], name, values, layout])) + ' |')
lines += ['', '## 菜单文案（只能证明资源存在，不能证明运行时位置）', '', '| 资源键 | 文案 |', '| --- | --- |']
for key, value in resources.items():
    if key.startswith('Menu'):
        lines.append(f"| {key} | {value.replace('|', '/') .replace(chr(10), '<br>')} |")
(OUT / 'INDEX.md').write_text('\n'.join(lines) + '\n', encoding='utf-8')
print(f'Validated {len(pages)} XML files; indexed {sum(len(p["nodes"]) for p in pages)} nodes; {len(resources)} strings.')
