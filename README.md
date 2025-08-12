# Image Type Converter

A simple WPF desktop app that lets you quickly convert multiple images to different formats — all in one place.

## Features

* **Drag & Drop** images directly into the app.
* **Multiple input formats** supported: PNG, JPG, JPEG, BMP, GIF, TIFF.
* **Batch conversion** with a single click.
* **Live progress display** for each file.
* **Preview thumbnails** before conversion.
* **Choose output format** per image.
* **Download individually** or **download all** converted images at once.

## How It Works

1. **Add Images**

   * Drag & drop images into the app, or click **Select Images** to browse.
2. **Choose Output Format**

   * For each image, select the desired format (PNG, JPEG, BMP, GIF, TIFF).
3. **Convert**

   * Click **Convert** to start. Progress bars show conversion status.
4. **Download**

   * Download individual files or click **Download All** to save everything to a folder.

## Tech Stack

* **Language:** C# (.NET)
* **UI Framework:** WPF (Windows Presentation Foundation)
* **Libraries:** Microsoft.Win32 for file dialogs, System.Windows.Media.Imaging for image processing.

## Requirements

* Windows OS
* .NET Desktop Runtime (version matching your build)

## Build & Run

1. Open the solution in **Visual Studio**.
2. Build the project.
3. Run the application.