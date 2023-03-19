import os
import tkinter as tk
from tkinter import filedialog
import argparse
from PIL import Image


def main():
    parser = argparse.ArgumentParser(description='gilde-decoder')
    parser.add_argument('-i', '--input', help='input path')
    parser.add_argument('-o', '--output', help='output path')

    args = parser.parse_args()

    if not args.input:
        root = tk.Tk()
        root.withdraw()
        args.input = filedialog.askdirectory()
        root.destroy()
        args.input = args.input.replace('/', '\\')

    if not args.output:
        args.output = os.path.join(os.getcwd(), 'output')

    if not os.path.exists(args.input):
        print('input path does not exist')
        return

    if not os.path.exists(args.output):
        os.mkdir(args.output)

    gfx_path = os.path.join(args.input, "gfx")
    gfx_file_path = os.path.join(gfx_path, "Gilde_add_on_german.gfx")

    decode_graphics(gfx_file_path, args.output)


def decode_graphics(input_path: str, output_path: str):
    if input_path is None or output_path is None:
        raise ValueError("Input and output paths must be specified")

    if not os.path.exists(input_path):
        raise FileNotFoundError("Input file not found")

    output_graphics_path = os.path.join(output_path, "graphics")

    if not os.path.exists(output_graphics_path):
        os.mkdir(output_graphics_path)

    with open(input_path, "rb") as file:
        header_data = file.read(4)

        graphics_count = int.from_bytes(header_data, byteorder="little", signed=False)

        graphics_headers = []

        for i in range(graphics_count):
            graphics_header_data = file.read(84)
            graphics_name = graphics_header_data[:48].decode("ascii").strip("\x00")
            graphics_start_address = int.from_bytes(graphics_header_data[48:52], byteorder="little", signed=False)
            shapbank_size = int.from_bytes(graphics_header_data[56:60], byteorder="little", signed=False)
            graphics_width = int.from_bytes(graphics_header_data[80:82], byteorder="little", signed=False)
            graphics_height = int.from_bytes(graphics_header_data[82:84], byteorder="little", signed=False)

            graphics_headers.append({
                "name": graphics_name,
                "start_address": graphics_start_address,
                "size": shapbank_size,
                "width": graphics_width,
                "height": graphics_height
            })

        for graphic_header in graphics_headers:
            file.seek(graphic_header["start_address"])

            shapbank_data = file.read(graphic_header["size"])

            graphics_count = int.from_bytes(shapbank_data[42:44], byteorder="little", signed=False)

            # What is this information used for?
            max_width = int.from_bytes(shapbank_data[44:46], byteorder="little", signed=False)
            max_width = int.from_bytes(shapbank_data[46:48], byteorder="little", signed=False)

            # Shapbank size is stored twice? In the file header and in the Shapbank header
            shapbank_size = int.from_bytes(shapbank_data[48:52], byteorder="little", signed=False)

            graphics_size_offset = 69
            graphics_offsets = []
            for i in range(graphics_count):
                offset = graphics_size_offset + 4 * i
                graphics_offsets.append(
                    int.from_bytes(shapbank_data[offset:offset + 4], byteorder="little", signed=False))

            x = 0
            for i in graphics_offsets:
                graphic_data = bytearray()

                file.seek(graphic_header["start_address"] + i)
                graphic_size = int.from_bytes(file.read(4), byteorder="little", signed=False)
                num1 = file.read(2)
                graphic_width = int.from_bytes(file.read(2), byteorder="little", signed=False)
                num2 = file.read(2)
                graphic_height = int.from_bytes(file.read(2), byteorder="little", signed=False)
                num3 = file.read(2) # Always 0x0002
                num4 = file.read(2)
                num5 = file.read(2)
                graphic_width_2 = file.read(2)
                graphic_height_2 = file.read(2)
                num6 = file.read(2)
                num7 = file.read(2)
                num8 = file.read(2)
                num9 = file.read(2)
                file.seek(8, os.SEEK_CUR)
                num10 = file.read(4)
                graphic_size_without_footer = file.read(4) # Might help to calculate the actual graphic size
                num12 = file.read(4) # Seems to be related to the graphic dimensions
                
                # This method only works for a subset of graphics
                border_pixel_count = 4
                for j in range(graphic_height):
                    file.read(4 * 3)
                    graphic_data += bytearray(file.read(graphic_width * 3))

                img = Image.frombytes(mode="RGB", size=(graphic_width, graphic_height), data=bytes(graphic_data))

                if x == 0:
                    file_path = os.path.join(output_graphics_path, graphic_header["name"] + ".bmp")
                else:
                    file_path = os.path.join(output_graphics_path, graphic_header["name"] + "+" + str(x) + ".bmp")

                img.save(file_path)
                x += 1


if __name__ == "__main__":
    main()
