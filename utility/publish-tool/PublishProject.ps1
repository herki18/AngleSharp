# PublishProjects.ps1

$projects = @(
    @{
        Name = "LayoutEngine"
        ProjectPath = "D:\Development\Github\AngleSharp\src\LayoutEngine.Core\LayoutEngine.Core.csproj"
        Framework = "netstandard2.1"
        PublishDir = "D:\Development\Github\AngleSharp\publish\LayoutEngine.Core"
    }
)

foreach ($proj in $projects) {
    Write-Host "Publishing $($proj.Name)..."
    dotnet publish $proj.ProjectPath -c Debug -f $proj.Framework -o $proj.PublishDir
}
