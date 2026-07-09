"""
从单一源 PNG 一键生成三平台应用图标

输入  :Assets/zicon.png (可自定义)
输出  :Assets/icon.png  / icon.ico  / icon.icns

用法  :
    python3 Assets/build_icons.py                       # 默认 Assets/zicon.png
    python3 Assets/build_icons.py --input path/foo.png  # 自定义源
    python3 Assets/build_icons.py --size 1024           # PNG 目标边长,默认 512

约定  :
    * 源图建议 ≥ 1024x1024,RGBA 或 RGB,正方形最佳(非正方形会居中补透明边)
    * ICO 内置 16/24/32/48/64/128/256 共 7 档
    * ICNS 内置 16/32/64/128/256/512 共 6 档(Apple PNG 编码)
"""
import argparse
import io
import os
import struct
import sys

from PIL import Image, ImageOps

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
DEFAULT_INPUT = os.path.join(SCRIPT_DIR, "zicon.png")
DEFAULT_OUTPUT_DIR = SCRIPT_DIR

# ICO:Windows 多分辨率(像素)
ICO_SIZES = [16, 24, 32, 48, 64, 128, 256]
# ICNS:Apple 规范类型码 + 对应像素
ICNS_TABLE = [
    (16, "icp4"),
    (32, "icp5"),
    (64, "icp6"),
    (128, "ic07"),
    (256, "ic08"),
    (512, "ic09"),
]


def prepare_square(img: Image.Image, target: int) -> Image.Image:
    """规范化源图:
    - 转 RGBA
    - 非正方形 → 居中补透明边
    - 高质量缩放到 target x target
    """
    img = img.convert("RGBA")
    if img.size != (target, target):
        # 先按比例缩放到能完整放入 target×target 的最大尺寸
        scale = min(target / img.size[0], target / img.size[1])
        new_w = max(1, round(img.size[0] * scale))
        new_h = max(1, round(img.size[1] * scale))
        resized = img.resize((new_w, new_h), Image.LANCZOS)
        canvas = Image.new("RGBA", (target, target), (0, 0, 0, 0))
        canvas.paste(resized, ((target - new_w) // 2, (target - new_h) // 2))
        img = canvas
    return img


def build_icon_png(source: Image.Image, size: int, out_path: str) -> None:
    """生成主 PNG(Linux / 通用)。"""
    img = prepare_square(source, size)
    img.save(out_path, "PNG", optimize=True)
    print(f"  [png ] {out_path}  ({size}x{size}, {os.path.getsize(out_path)} bytes)")


def build_icon_ico(source: Image.Image, out_path: str) -> None:
    """生成多分辨率 ICO(Windows)。"""
    frames = [prepare_square(source, s) for s in ICO_SIZES]
    frames[0].save(
        out_path,
        format="ICO",
        sizes=[(s, s) for s in ICO_SIZES],
        append_images=frames[1:],
    )
    print(f"  [ico ] {out_path}  (sizes={ICO_SIZES}, {os.path.getsize(out_path)} bytes)")


def build_icon_icns(source: Image.Image, out_path: str) -> None:
    """生成多分辨率 ICNS(macOS, PNG-encoded)。"""

    def png_block(code: str, png_bytes: bytes) -> bytes:
        return code.encode("ascii") + struct.pack(">I", 8 + len(png_bytes)) + png_bytes

    blocks = []
    for size, code in ICNS_TABLE:
        img = prepare_square(source, size)
        buf = io.BytesIO()
        img.save(buf, "PNG", optimize=True)
        blocks.append(png_block(code, buf.getvalue()))

    body = b"".join(blocks)
    with open(out_path, "wb") as f:
        f.write(b"icns" + struct.pack(">I", 8 + len(body)) + body)
    print(
        f"  [icns] {out_path}  (sizes={[s for s, _ in ICNS_TABLE]}, "
        f"{os.path.getsize(out_path)} bytes)"
    )


def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(
        description="从单一 PNG 源生成三平台应用图标 (png / ico / icns)"
    )
    p.add_argument(
        "--input",
        "-i",
        default=DEFAULT_INPUT,
        help=f"源 PNG 路径(默认: {DEFAULT_INPUT})",
    )
    p.add_argument(
        "--size",
        "-s",
        type=int,
        default=512,
        help="主 PNG 输出边长(默认 512)",
    )
    p.add_argument(
        "--out-dir",
        "-o",
        default=DEFAULT_OUTPUT_DIR,
        help=f"输出目录(默认: {DEFAULT_OUTPUT_DIR})",
    )
    return p.parse_args()


def main() -> int:
    args = parse_args()

    if not os.path.isfile(args.input):
        print(f"[err] 找不到源图: {args.input}", file=sys.stderr)
        print(
            "      请将你的应用图标重命名为 zicon.png 放到 Assets/ 下,"
            "或用 --input 指定其他路径。",
            file=sys.stderr,
        )
        return 1

    print(f"[src] {args.input}")
    try:
        source = Image.open(args.input)
        source.load()  # 立即解码,避免后面重复读 IO
    except Exception as e:
        print(f"[err] 无法打开源图: {e}", file=sys.stderr)
        return 2

    print(f"[out] {args.out_dir}")
    os.makedirs(args.out_dir, exist_ok=True)

    out_png = os.path.join(args.out_dir, "icon.png")
    out_ico = os.path.join(args.out_dir, "icon.ico")
    out_icns = os.path.join(args.out_dir, "icon.icns")

    build_icon_png(source, args.size, out_png)
    build_icon_ico(source, out_ico)
    build_icon_icns(source, out_icns)

    print("[done] 三平台图标已生成。")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())