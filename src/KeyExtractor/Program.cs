// using System;
// using System.IO;
// using System.Reflection;
//
// namespace KeyExtractor;
//
// class Program
// {
//     static void Main(string[] args)
//     {
//         // Path to your SNK file
//         string keyFilePath = @"D:\Development\Github\AngleSharp\src\LayoutEngine.snk";
//
//         // Read the key file
//         byte[] keyFileBytes = File.ReadAllBytes(keyFilePath);
//
//         // Create a StrongNameKeyPair from the key file
//         var keyPair = new StrongNameKeyPair(keyFileBytes);
//
//         // Get the public key
//         byte[] publicKey = keyPair.PublicKey;
//
//         // Convert to the format needed for InternalsVisibleTo
//         string publicKeyString = BitConverter.ToString(publicKey).Replace("-", "");
//
//         Console.WriteLine("[assembly: InternalsVisibleTo(\"Infrastructure.CacheManager.Tests, PublicKey=" + publicKeyString + "\")]");
//         Console.ReadLine();
//     }
// }