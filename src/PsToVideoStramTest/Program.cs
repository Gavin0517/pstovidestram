// See https://aka.ms/new-console-template for more information
using PsToVideStream;
using System;
using System.Collections;

class Program
{
    static void Main(string[] args)
    {
        var psBuffer = File.ReadAllBytes(@$"{Environment.CurrentDirectory}\testdata\3.ps").ToList();
        PsToVideoStream psToVideoStream = new PsToVideoStream();
        var videoStreams=psToVideoStream.GeVideoStreamFromPs(psBuffer);
        if (videoStreams is not null)
            File.WriteAllBytes(Path.Combine(Environment.CurrentDirectory, "3.h265"), videoStreams.ToArray());
        Console.WriteLine("Hello, World!");
    }
}

