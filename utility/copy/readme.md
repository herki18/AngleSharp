# CopyBuildFiles Utility

This utility automates copying build output files from one directory to another, as defined in a JSON configuration file.  
It is especially useful for .NET/C# projects where build artifacts need to be moved between project folders.

---

## Features

- **Configurable:** Define any number of source/destination pairs in a JSON file.
- **Recursive:** Copies all files and subfolders from each source.
- **Overwrite:** Existing files in the destination are overwritten.
- **Automatic Directory Creation:** Destination folders are created if they do not exist.
- **Progress Feedback:** Reports how many files were copied for each operation.

---

## Prerequisites

- **PowerShell 5.1+** (Windows PowerShell) or **PowerShell Core** (`pwsh`, cross-platform)
- Sufficient permissions to read from source and write to destination directories

---

## Usage

1. **Edit the Configuration File**

   Create or edit a JSON file (e.g., `copy-config.json`) with your source and destination paths.  
   Example:

   ```json
   [
       {
           "Source": "C:\\dev\\Git\\Training\\AngleSharp\\src\\LayoutEngine\\bin\\Debug\\netstandard2.1",
           "Destination": "C:\\dev\\Git\\Training\\JSXEngine\\src\\JSXEngine4\\Assets\\Plugins"
       }
   ]
   ```

2. **Run the Script**

   Open a PowerShell terminal in the directory containing the script and run:

   ```powershell
   .\CopyBuildFiles.ps1 -ConfigFile "copy-config.json"
   ```

   - If your config file is in a different location, provide the full or relative path.

---

## Script Details

- For each source/destination pair:
  - Checks if the source exists.
  - Creates the destination directory if it does not exist.
  - Recursively copies all files and subfolders from source to destination, overwriting existing files.
  - Reports the number of files copied.

---

## Example Output

```
Created destination directory: C:\dev\Git\Training\JSXEngine\src\JSXEngine4\Assets\Plugins
Copied 42 file(s) from C:\dev\Git\Training\AngleSharp\src\LayoutEngine\bin\Debug\netstandard2.1 to C:\dev\Git\Training\JSXEngine\src\JSXEngine4\Assets\Plugins
```

---

## Troubleshooting

- **Config file not found:**  
  Ensure the path to your JSON config file is correct.
- **Source does not exist:**  
  Check that the source directory exists and is spelled correctly.
- **Permission errors:**  
  Run PowerShell as Administrator if you encounter access denied errors.

---

## Customization

- Add more source/destination pairs to the JSON array as needed.
- The script can be extended to support logging, dry-run, file filters, etc.

---

## License

This utility is provided as-is, without warranty.  
Feel free to modify and use it in your projects.

---

**Questions or suggestions?**  
Open an issue or contact the maintainer.

---