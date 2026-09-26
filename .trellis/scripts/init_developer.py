#!/usr/bin/env python3
"""
为工作流初始化开发者。

用法：
    python3 init_developer.py <开发者名称>

此命令创建：
    - 保存开发者信息的 .trellis/.developer 文件
    - .trellis/workspace/<name>/ 目录结构
"""

from __future__ import annotations

import sys

from common.paths import (
    DIR_WORKFLOW,
    FILE_DEVELOPER,
    get_developer,
)
from common.developer import init_developer


def main() -> None:
    """命令行入口。"""
    if len(sys.argv) < 2:
        print(f"用法： {sys.argv[0]} <开发者名称>")
        print()
        print("示例：")
        print(f"  {sys.argv[0]} john")
        sys.exit(1)

    name = sys.argv[1]

    # 检查是否已初始化
    existing = get_developer()
    if existing:
        print(f"开发者已经初始化：{existing}")
        print()
        print(f"如需重新初始化，请先删除 {DIR_WORKFLOW}/{FILE_DEVELOPER}")
        sys.exit(0)

    if init_developer(name):
        sys.exit(0)
    else:
        sys.exit(1)


if __name__ == "__main__":
    main()
