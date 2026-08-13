# ImageComparator
An image comparing console application we built a few years ago.

It also has a thumbnailer.

## Comparison strategies

The comparer now supports multiple strategies:

- `legacy` (original dominant-channel logic)
- `mad` (mean absolute pixel difference on normalized images)
- `dhash` (perceptual difference hash)
- `auto` (default; chooses a strategy based on image characteristics)

## Usage

```bash
dotnet run --project /home/runner/work/ImageComparator/ImageComparator/ImageComparator/ImageComparator.csproj -- <goodDir> <badDir> [fileType] [--strategy=auto|legacy|mad|dhash] [--benchmark] [--benchmark-iterations=25]
```

When `--benchmark` is enabled, the app prints per-strategy timing and similarity output for the first compared image pair.

## Tests

Run:

```bash
dotnet run --project /home/runner/work/ImageComparator/ImageComparator/ImageComparator.Tests/ImageComparator.Tests.csproj
```
