<#
.SYNOPSIS
Runs the 'repomix' Node.js package (via npx) based on tasks defined in a JSON configuration file,
allowing per-directory overrides for options. Uses Start-Process with quoted path arguments.

.DESCRIPTION
This script reads configuration settings from a JSON file which defines global settings and a list of tasks.
Each task specifies a target directory and can optionally override global settings like repomix options,
ignore patterns, and the output filename pattern. The script iterates through the tasks and executes
'npx repomix' for each one using the calculated effective settings via Start-Process targeting npx.cmd,
with path arguments explicitly quoted in the argument list.

.NOTES
Author: Gemini
Date:   2025-04-18
Version: 3.0 (Return to Start-Process, add explicit quotes for path args)
Requires: PowerShell 5.1 or later.
**Prerequisite:** Node.js and npm must be installed and available in the system's PATH.
**Configuration:** Requires a JSON configuration file (default: .\repomix_config.json) with 'globalSettings' and 'tasks' structure. See example JSON.

.PARAMETER ConfigPath
The path to the JSON configuration file.
Default is '.\repomix_config.json'.

.EXAMPLE
.\Run-Repomix-v2.ps1
Runs the script using settings and tasks from '.\repomix_config.json'.

.EXAMPLE
.\Run-Repomix-v2.ps1 -ConfigPath "C:\MyConfigs\project_alpha_config.json"
Runs the script using settings and tasks from a specific configuration file.

#>
param (
    # --- Configuration File Path ---
    [string]$ConfigPath = ".\repomix_config.json"
    # --- End Configuration File Path ---
)

# --- Script Body ---
Write-Host "--- Repomix PowerShell Runner v3.0 ---"

# Verify and find the npx.cmd command path
$npxCmdCommand = Get-Command npx.cmd -ErrorAction SilentlyContinue
if (-not $npxCmdCommand) {
    Write-Error "[ERROR] npx.cmd command not found. Please ensure Node.js and npm are installed and added to your system's PATH."
    exit 1 # Exit the script if npx.cmd cannot be found
} else {
     Write-Host "[INFO] Found npx.cmd at: $($npxCmdCommand.Source). Will use Start-Process to run repomix via npx.cmd."
}
$npxCmdPath = $npxCmdCommand.Source

# --- Load Configuration from JSON ---
$ResolvedConfigPath = Resolve-Path -Path $ConfigPath -ErrorAction SilentlyContinue
Write-Host "[INFO] Attempting to load configuration from: $ResolvedConfigPath"
if (-not $ResolvedConfigPath -or -not (Test-Path -Path $ResolvedConfigPath -PathType Leaf)) {
    Write-Error "[ERROR] Configuration file not found at '$ConfigPath' (Resolved: '$ResolvedConfigPath')."
    exit 1
}

$config = $null
try {
    # Read JSON content
    $jsonContent = Get-Content -Path $ResolvedConfigPath -Raw -ErrorAction Stop
    # Parse JSON content
    $config = $jsonContent | ConvertFrom-Json -ErrorAction Stop
} catch {
    Write-Error "[ERROR] Failed to read or parse JSON configuration file '$ResolvedConfigPath'. Please check its format and permissions. Error: $($_.Exception.Message)"
    exit 1
}

# Validate essential config structure (simplified checks)
if (-not $config.globalSettings -or -not $config.tasks) {
     Write-Error "[ERROR] JSON configuration file '$ResolvedConfigPath' is missing required top-level properties ('globalSettings' and 'tasks')."
     exit 1
}
if (-not $config.globalSettings.outputBaseDirectory) {
     Write-Error "[ERROR] JSON configuration file '$ResolvedConfigPath' is missing 'globalSettings.outputBaseDirectory'."
     exit 1
}
if (-not $config.tasks -is [array]) {
     Write-Error "[ERROR] JSON configuration file '$ResolvedConfigPath' property 'tasks' must be an array."
     exit 1
}
# --- End Load Configuration ---


# Ensure the base output directory exists
$OutputBaseDirectory = $config.globalSettings.outputBaseDirectory
try {
    if (-not (Test-Path -Path $OutputBaseDirectory -PathType Container)) {
        Write-Host "[INFO] Creating base output directory: $OutputBaseDirectory"
        New-Item -Path $OutputBaseDirectory -ItemType Directory -Force -ErrorAction Stop | Out-Null
    }
} catch {
     Write-Error "[ERROR] Failed to create or access output directory '$OutputBaseDirectory' specified in config. Please check the path and permissions. Error: $($_.Exception.Message)"
     exit 1
}

Write-Host "[INFO] Starting repomix execution process via npx based on tasks..."
Write-Host "[INFO] Output files will be saved in: $(Resolve-Path $OutputBaseDirectory)"
Write-Host "[INFO] Processing $($config.tasks.Count) task(s)..."

# Process each task individually
foreach ($task in $config.tasks) {


    if ($task.PSObject.Properties.Name -contains 'skip' -and $task.skip -eq $true) {
        Write-Host "[INFO] Skipping task for directory: $($task.targetDirectory) (skip=true in config)"
        continue
    }

    # Validate task has targetDirectory
    if (-not $task -or -not $task.PSObject.Properties.Name -contains 'targetDirectory' -or -not $task.targetDirectory) {
         Write-Warning "[WARN] Skipping task because 'targetDirectory' is missing or empty in the config file."
         continue
    }
    $dir = $task.targetDirectory

    # Resolve the target directory path for better error messages
    $ResolvedDir = Resolve-Path -Path $dir -ErrorAction SilentlyContinue

    if ($ResolvedDir -and (Test-Path -Path $ResolvedDir -PathType Container)) {
        Write-Host "`n[INFO] Processing Task for directory: $ResolvedDir"

        # --- Determine Effective Settings for this Task ---
        # Start with global settings and override with task-specific settings if they exist

        # Output Filename Pattern
        $effectiveFileNamePattern = $config.globalSettings.outputFileNamePattern # Default to global
        if ($task.PSObject.Properties.Name -contains 'outputFileNamePattern' -and $task.outputFileNamePattern) {
            $effectiveFileNamePattern = $task.outputFileNamePattern
            Write-Host "  [INFO] Using task-specific filename pattern: $effectiveFileNamePattern"
        }

        # Repomix Options (Merge global and task-specific) - Use hashtables
        $effectiveRepomixOptions = @{}

        if ($config.globalSettings.PSObject.Properties.Name -contains 'repomixOptions' -and $config.globalSettings.repomixOptions) {
            $config.globalSettings.repomixOptions.PSObject.Properties | ForEach-Object {
                $effectiveRepomixOptions[$_.Name] = $_.Value
            }
        }

        if ($task.PSObject.Properties.Name -contains 'repomixOptions' -and $task.repomixOptions) {
             Write-Host "  [INFO] Applying task-specific repomix option overrides..."
             $task.repomixOptions.PSObject.Properties | ForEach-Object {
                 $effectiveRepomixOptions[$_.Name] = $_.Value
                 Write-Host "    - $($_.Name) = $($_.Value)"
             }
        }

        # Additional Include Patterns (Task overrides global completely if specified)
        $effectiveIncludePatterns = $null
        if ($config.globalSettings.PSObject.Properties.Name -contains 'additionalIncludePatterns') { 
            $effectiveIncludePatterns = $config.globalSettings.additionalIncludePatterns 
        }
        if ($task.PSObject.Properties.Name -contains 'additionalIncludePatterns') {
            $effectiveIncludePatterns = $task.additionalIncludePatterns
            Write-Host "  [INFO] Using task-specific include patterns defined in config."
        }

        # Additional Ignore Patterns (Task overrides global completely if specified)
        $effectiveIgnorePatterns = $null
        if ($config.globalSettings.PSObject.Properties.Name -contains 'additionalIgnorePatterns') { $effectiveIgnorePatterns = $config.globalSettings.additionalIgnorePatterns }
        if ($task.PSObject.Properties.Name -contains 'additionalIgnorePatterns') {
            $effectiveIgnorePatterns = $task.additionalIgnorePatterns
            Write-Host "  [INFO] Using task-specific ignore patterns defined in config."
        }
        # --- End Determine Effective Settings ---


        # Determine the output filename using the effective pattern
        $dirName = (Split-Path -Path $ResolvedDir -Leaf)
        $outputFileName = $null
        try {
            $outputFileName = $effectiveFileNamePattern -replace '{dirName}', $dirName
        } catch {
             Write-Error "[ERROR] Error processing outputFileNamePattern '$effectiveFileNamePattern' for task '$ResolvedDir'. Error: $($_.Exception.Message)"
             continue
        }
        if (-not $outputFileName) { # Check if null or empty
             Write-Error "[ERROR] Failed to generate output filename for task '$ResolvedDir' using pattern '$effectiveFileNamePattern'."
             continue
        }

        $outputFilePath = Join-Path -Path $OutputBaseDirectory -ChildPath $outputFileName
        Write-Host "  [INFO] Output file for this task: $outputFilePath"


        # Construct the command line arguments LIST for Start-Process
        # Arguments for npx.cmd: the first argument is the package to run ('repomix'), followed by its options.
        $argumentList = @("repomix") # Command for npx to execute

        # FIXED: Add explicit simple quotes around paths for Start-Process argument list
        $argumentList += "-o", "`"$outputFilePath`""

        # Add repomix options dynamically from the effective options hashtable
        if ($effectiveRepomixOptions -and $effectiveRepomixOptions.Count -gt 0) {
             $effectiveRepomixOptions.GetEnumerator() | ForEach-Object {
                 $optionName = $_.Key
                 $optionValue = $_.Value
                 
                 # Robust camelCase to kebab-case conversion
                 $kebab = ""
                 $optionName.ToCharArray() | ForEach-Object {
                     if ([System.Char]::IsUpper($_)) { $kebab += "-" + $_.ToString().ToLower() }
                     else { $kebab += $_ }
                 }
                 $flagName = "--" + $kebab

                 # Handle boolean flags (true means add flag, false means omit)
                 if ($optionValue -is [bool]) {
                     if ($optionValue -eq $true) { $argumentList += $flagName }
                 } elseif ($optionValue -ne $null) {
                     # Handle flags that take a value (like --style)
                     $argumentList += $flagName
                     $argumentList += $optionValue # Add value as separate argument element
                 }
             }
        }

        # Add include patterns if they exist and the array is not empty
        if ($effectiveIncludePatterns -and $effectiveIncludePatterns.Count -gt 0) {
            $includeString = $effectiveIncludePatterns -join ','
            $argumentList += "--include"
            $argumentList += $includeString
        }

        # Add ignore patterns if they exist and the array is not empty
        if ($effectiveIgnorePatterns -and $effectiveIgnorePatterns.Count -gt 0) {
            $ignoreString = $effectiveIgnorePatterns -join ','
            $argumentList += "-i"
            $argumentList += $ignoreString
        }

        # Add the target directory path at the end
        # FIXED: Add explicit simple quotes around paths for Start-Process argument list
        $argumentList += "`"$ResolvedDir`""

        # Log the command elements for debugging (approximation)
        $logCommandString = "`"$npxCmdPath`" $($argumentList -join ' ')" # Simple join for logging
        Write-Host "  [INFO] Executing via Start-Process: $logCommandString" # Log command before execution

        # Execute npx repomix using Start-Process and the argument LIST
        $process = $null
        try {
            # Use Start-Process with npx.cmd: waits for completion, runs in current window
            $process = Start-Process -FilePath $npxCmdPath -ArgumentList $argumentList -Wait -NoNewWindow -PassThru -ErrorAction Continue

            # Check if process object was returned and check ExitCode
            if ($process -eq $null) {
                 Write-Error "[ERROR] Start-Process failed to launch or return process object for task '$ResolvedDir'."
            } elseif ($process.ExitCode -ne 0) {
                 Write-Warning "[WARN] Repomix process exited with non-zero code: $($process.ExitCode) for task '$ResolvedDir'. Check repomix output above."
            } else {
                 Write-Host "  [SUCCESS] Repomix command completed successfully for task '$ResolvedDir'."
            }
        } catch {
            # Catch errors from Start-Process execution itself (e.g., file not found, permissions)
            Write-Error "[ERROR] Error executing Start-Process for npx repomix task '$ResolvedDir'. PowerShell Error: $($_.Exception.Message)"
        }

    } else {
        Write-Warning "[WARN] Target directory not found or is not a directory for task: '$dir' (Resolved: '$ResolvedDir'). Skipping."
    }
} # End Foreach ($task in $config.tasks)

Write-Host "`n[INFO] Repomix execution process finished."
