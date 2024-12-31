using CommandLine;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

Parser.Default.ParseArguments<Options>(args)
    .WithParsed(RunOptions)
    .WithNotParsed(HandleParseError);

return;

void RunOptions(Options option)
{
    Console.WriteLine($"Framerate: {option.Framerate}");
    Console.WriteLine($"Scale: {option.Scale}");
    Console.WriteLine($"Input: {option.Input}");
    Console.WriteLine($"Output: {option.Output}");

    var frameDelay = 100 / option.Framerate;
    if (frameDelay < 1)
    {
        Console.WriteLine("Framerate is too high");
        return;
    }

    var (width, height) = GetScale(option.Scale);

    var files = EnumerateFiles(option.Input);
    if (files.Length == 0)
    {
        Console.WriteLine("No files found");
        return;
    }

    Console.WriteLine("Files found:");
    foreach (var file in files) Console.WriteLine(file);

    Console.WriteLine("Loading images");
    var images = new Image<Rgba32>[files.Length];
    for (var i = 0; i < files.Length; i++) images[i] = Image.Load<Rgba32>(files[i]);
    if (width == -1) width = images.Max(x => x.Width);
    if (height == -1) height = images.Max(x => x.Height);

    Console.WriteLine("Resizing images");
    foreach (var t in images)
        if (t.Width != width || t.Height != height)
            t.Mutate(x => x.Resize(width, height));

    Console.WriteLine("Creating gif");
    using var gif = new Image<Rgba32>(width, height);
    gif.Metadata.GetGifMetadata().RepeatCount = 0;
    foreach (var t in images)
    {
        var frame = gif.Frames.AddFrame(t.Frames.RootFrame);
        frame.Metadata.GetGifMetadata().FrameDelay = frameDelay;
    }

    gif.Frames.RemoveFrame(0);
    gif.SaveAsGif(option.Output);

    Console.WriteLine("Gif created");
}

void HandleParseError(IEnumerable<Error> errors)
{
    foreach (var error in errors) Console.WriteLine(error);
}

string[] EnumerateFiles(string input)
{
    var dir = Path.GetDirectoryName(input);
    var pattern = Path.GetFileName(input);
    if (string.IsNullOrEmpty(dir)) dir = Directory.GetCurrentDirectory();
    return Directory.GetFiles(dir, pattern).OrderBy(x => x).ToArray();
}

(int, int) GetScale(string scale)
{
    var parts = scale.Split('*');
    if (parts.Length != 2) return (-1, -1);
    if (!int.TryParse(parts[0], out var width)) width = -1;
    if (!int.TryParse(parts[1], out var height)) height = -1;
    return (width, height);
}

internal class Options
{
    [Option('f', "framerate", Required = false, Default = 30, HelpText = "Set the framerate of the gif")]
    public int Framerate { get; set; }

    [Option('s', "scale", Required = false, Default = "-1*-1", HelpText = "Set the scale of the gif")]
    public string Scale { get; set; } = "-1*-1";

    [Option('i', "input", Required = true, HelpText = "Set the input files")]
    public string Input { get; set; } = string.Empty;

    [Option('o', "output", Required = true, HelpText = "Set the output file")]
    public string Output { get; set; } = string.Empty;
}