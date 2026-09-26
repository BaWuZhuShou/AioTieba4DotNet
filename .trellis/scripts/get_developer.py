#!/usr/bin/env python3
"""
获取当前开发者名称。

此入口封装了 common/paths.py。
"""

from __future__ import annotations

import sys

from common.paths import get_developer


def main() -> None:
    """命令行入口。"""
    developer = get_developer()
    if developer:
        print(developer)
    else:
        print("开发者尚未初始化", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
