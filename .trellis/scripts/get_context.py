#!/usr/bin/env python3
"""获取 AI 代理的会话上下文。

用法：
    python3 get_context.py           输出文本格式的上下文
    python3 get_context.py --json    输出 JSON 格式的上下文"""

from __future__ import annotations

from common.git_context import main


if __name__ == "__main__":
    main()
