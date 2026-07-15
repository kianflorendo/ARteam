import os
from PIL import Image, ImageDraw

def make_circular_transparent(folder_path):
    for filename in os.listdir(folder_path):
        if filename.endswith(".jpg"):
            filepath = os.path.join(folder_path, filename)
            img = Image.open(filepath).convert("RGBA")
            
            # The images might not have the circle going perfectly to the edges. 
            # We'll draw an ellipse matching the image boundaries to crop the square corners.
            # (Assuming the generated image has the circle touching the edges)
            mask = Image.new('L', img.size, 0)
            draw = ImageDraw.Draw(mask)
            draw.ellipse((0, 0, img.size[0], img.size[1]), fill=255)
            
            # Apply mask
            result = img.copy()
            result.putalpha(mask)
            
            # Save as PNG
            new_filename = filename.replace(".jpg", ".png")
            new_filepath = os.path.join(folder_path, new_filename)
            result.save(new_filepath, "PNG")
            print(f"Processed {filename} -> {new_filename}")
            
            # Optionally remove the original jpg
            os.remove(filepath)

make_circular_transparent('.')
