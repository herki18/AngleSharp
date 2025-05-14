Okay, here is the content of the README file printed directly:

```markdown
# Repomix PowerShell Runner (v2)

## Overview

This PowerShell script (`Run-Repomix-v2.ps1`) automates the process of running the [`repomix`](https://github.com/yamadashy/repomix) Node.js tool across multiple directories. It uses a JSON configuration file to define target directories, output locations, and various `repomix` options, allowing for fine-grained control over each directory processed via task-specific overrides.

The script leverages `npx` to execute `repomix`, ensuring the latest version is used without requiring a global installation.

## Prerequisites

* **Node.js and npm:** Required to run `npx` and `repomix`. Download from [nodejs.org](https://nodejs.org/). Verify installation with `node -v` and `npm -v`.
* **PowerShell:** Version 5.1 or later (standard on modern Windows).

## Files

1.  **`Run-Repomix-v2.ps1`**: The main PowerShell script (or whatever you name it).
2.  **`repomix_config.json`** (Default Name): The JSON file where you define settings and tasks.

## JSON Configuration (`repomix_config.json`)

The script reads its configuration from a JSON file. Here's a breakdown of the structure:

```json
{
  "globalSettings": {
    "outputBaseDirectory": "path\\to\\your\\output\\folder",
    "outputFileNamePattern": "{dirName}_output.txt",
    "repomixOptions": {
      "style": "xml", // Content format: "xml", "markdown", "plain"
      "removeComments": false,
      "removeEmptyLines": false,
      "noFileSummary": false, // Set true to exclude summary
      "noDirectoryStructure": false // Set true to exclude structure
      // Add other repomix boolean flags or value flags here
      // e.g., "compress": true, "tokenCountEncoding": "cl100k_base"
    },
    "additionalIgnorePatterns": [
      // Default ignore patterns applied to all tasks unless overridden
      "**/obj",
      "**/bin"
    ]
  },
  "tasks": [
    {
      "targetDirectory": "path\\to\\source\\directory1"
      // This task uses all globalSettings
    },
    {
      "targetDirectory": "path\\to\\source\\directory2",
      // Optional overrides for this specific task:
      "outputFileNamePattern": "{dirName}_custom_name.md",
      "repomixOptions": {
        "style": "markdown", // Override style
        "removeComments": true // Override removeComments
        // Other options like removeEmptyLines are inherited from globalSettings
      },
      "additionalIgnorePatterns": [
         // These patterns REPLACE the global ones for this task
         "**/node_modules",
         "**/*.log"
        ]
    }
    // Add more task objects as needed
  ]
}
```

**Key Points:**

* **`globalSettings`**: Defines default values used for all tasks.
    * `outputBaseDirectory`: Where all output files will be saved. (Required)
    * `outputFileNamePattern`: A template for output filenames. `{dirName}` will be replaced with the source directory's name. (Required)
    * `repomixOptions`: An object containing default settings corresponding to `repomix` command-line flags (e.g., `style`, `removeComments`, `noFileSummary`). Use the camelCase version of the flag name found in `repomix` documentation or config examples. The script converts these to `--kebab-case` flags.
    * `additionalIgnorePatterns`: Default list of glob patterns passed to `repomix` via the `-i` flag.
* **`tasks`**: An array of objects, each defining a directory to process. (Required, must be an array)
    * `targetDirectory`: The full path to the source code directory. (Required within each task object)
    * `outputFileNamePattern` (Optional): Overrides the global pattern for this task.
    * `repomixOptions` (Optional): Overrides specific options from `globalSettings.repomixOptions` for this task. Only include options you want to change.
    * `additionalIgnorePatterns` (Optional): If present, this list *replaces* the global list for this task. An empty array `[]` means no *additional* patterns are passed via `-i` for this task (though `repomix` defaults and `.gitignore` might still apply). If the property is omitted entirely from a task, the global setting is used.
* **Paths in JSON:** Use double backslashes (`\\`) for Windows paths. Ensure paths are correct and accessible.

## How to Run

1.  **Save Files:** Save the PowerShell script (e.g., `Run-Repomix-v2.ps1`) and the JSON configuration (`repomix_config.json`) to a location on your computer (e.g., in the same directory).
2.  **Edit Configuration:** Modify `repomix_config.json` with your actual directory paths, desired output location, and preferred `repomix` settings and overrides. **Verify JSON syntax carefully.** Online JSON validators can help.
3.  **Open PowerShell:** Launch a PowerShell terminal.
4.  **Navigate:** Use the `cd` command to navigate to the directory where you saved the script.
    ```powershell
    cd C:\path\to\your\scripts
    ```
5.  **Set Execution Policy (If Needed):** If you encounter errors about script execution being disabled, run the following command *in the current PowerShell session*:
    ```powershell
    Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force
    ```
6.  **Execute the Script:**
    * If your config file is named `repomix_config.json` and is in the same directory:
        ```powershell
        .\Run-Repomix-v2.ps1
        ```
    * If your config file has a different name or location:
        ```powershell
        .\Run-Repomix-v2.ps1 -ConfigPath "C:\path\to\your\custom_config.json"
        ```

The script will then process each task defined in the JSON file, printing informational messages, executing `npx repomix` with the appropriate settings, and saving the output files to the specified `outputBaseDirectory`. Review the console output for any warnings or errors from the script or from `repomix` itself.
```