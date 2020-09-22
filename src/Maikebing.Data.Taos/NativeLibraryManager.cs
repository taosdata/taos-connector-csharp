/*
 

https://github.com/olegtarasov/NativeLibraryManager



MIT License

Copyright (c) 2019 Oleg Tarasov

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.


Native dependency manager for .NET Standard libraries
This library helps you manage dependencies that you want to bundle with your .NET standard 
assembly. Originally it was developed to be used with native shared libraries, but you can 
bundle any file you want.

The main feature of this library is cross-platform support. You tell it which dependencies are 
for which platform, and LibraryManager will extract and load relevant files under each patform.


How to use the library
Pack your dependencies as embedded resources
Put your dependencies somewhere relative to your project. Let's assume you have one library compiled 
for each platform: TestLib.dll for Windows, libTestLib.so for Linux and libTestLib.dylib for macOs. 
Add these files as embedded resources to your .csproj as follows:

<Project Sdk="Microsoft.NET.Sdk">

    <!-- Some other stuff here -->

    <ItemGroup>
      <EmbeddedResource Include="libTestLib.dylib" />
      <EmbeddedResource Include="libTestLib.so" />
      <EmbeddedResource Include="TestLib.dll" />
    </ItemGroup>
</Project>
Now your dependencies will be compiled into your assembly as resources.

Use LibraryManager to specify and extract dependencies
private static void Main(string[] args)
{
    var accessor = new ResourceAccessor(Assembly.GetExecutingAssembly());
    var libManager = new LibraryManager(
        new LibraryItem(Platform.MacOs, Bitness.x64,
            new LibraryFile("libTestLib.dylib", accessor.Binary("libTestLib.dylib"))),
        new LibraryItem(Platform.Windows, Bitness.x64, 
            new LibraryFile("TestLib.dll", accessor.Binary("TestLib.dll"))),
        new LibraryItem(Platform.Linux, Bitness.x64,
            new LibraryFile("libTestLib.so", accessor.Binary("libTestLib.so"))));
    
    libManager.LoadNativeLibrary();

    // Library is loaded, other code here
}
Each LibraryItem specifies a bundle of files that should be extracted for a specific platform.
It this case we create 3 instances to support Windows, Linux and MacOs — all 64-bit. LibraryItem
takes any number of LibraryFile objects. With these objects you specify the extracted file name
and an actual binary file in the form of byte array. This is where ResourceAccessor comes in handy.

We should note that resource name you pass to ResourceAccessor is just a path to original file
relative to project root with slashes \\ replaced with dots . So, for example, if we place 
some file in Foo\Bar\lib.dll project folder, we would adderss it as:

accessor.Binary("Foo.Bar.lib.dll")
Target dependency directory
LibraryManager extracts your dependencies to current process' current directory. 
This is the only reliable way to use [DllImport] on all three platforms.

If your current directory isn't writable, you are generally out of luck. You can use 
an overload of LibraryManager's constructor which accepts a custom target directory, but then you 
need to do one of the following:

Ensure that target directory that you specify is discoverable by system library loader.
The safest bet is to ensure it's on your PATH before the whole process starts.
Enable explicit library loading with LibraryManager.LoadLibraryExplicit (read 
the next section for details). This will not work on MacOs. If your target path is not discoverable 
by system library loader, dlopen will succeed on MacOs, but your P/Invoke calls will fail. This problem 
can be mitigated by manually resolving function pointers, but this approach is not yet implemented in this library.
Explicit library loading
Warning! Explicit library loading on MacOs IS USELESS, and your P/Invoke call will fail unless 
library path is discoverable by system library loader (by adding target path to LD_LIBRARY_PATH or 
PATH before running your app, for example).

In previous versions of NativeLibraryManager the default behavior was to explicitly load every 
file using LoadLibraryEx on Windows and dlopen on Linux (explit loading wasn't implemented for MacOs). 
This approach was quite rigid and caused at least two problems:

There might have been some supporting files which didn't require explicit loading. You couldn't 
load some files and not load the others.
You should have observed a specific order in which you defined LibraryFiles if some of them were
dependent on others.
Starting from v. 1.0.21 explicit loading is disabled by default.

Nevertheless, sometimes you might want to load libraries explicitly. To do so, set 
LibraryManager.LoadLibraryExplicit to True before calling LibraryManager.LoadNativeLibrary().

You can also set LibraryFile.CanLoadExplicitly to False for supporting files, which you want 
to exclude from explicit loading.

When LibraryManager.LoadLibraryExplicit is True, LoadLibraryEx will be called to explicitly load l
ibraries on Windows, and dlopen will be called on Linux and MacOs.

Dependency load order with explicit loading
As mentioned earlier, there is a restriction when explicitly loading dependencies. 
If your native library depends on other native libraries, which you would also like to bundle with you assembly, 
you should observe a special order in which you specify LibraryFile items. You should put libraries with
no dependencies ("leaves") first, and dependent libraries last. Use ldd on Linux or Dependency Walker on Windows to discover the dependecies in your libraries.

Logging with Microsoft.Extensions.Logging
LibraryManager writes a certain amount of logs in case you would like to debug something. This library 
uses .NET Core Microsoft.Extensions.Logging abstraction, so in order to enable logging, just obtain an
instance of Microsoft.Extensions.Logging.ILoggerFactory and pass it as a parameter to LibraryManager constructor.




 */


using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;

namespace Maikebing.Data.Taos
{
    /// <summary>
    /// Process bitness.
    /// </summary>
    internal enum Bitness
    {
        /// <summary>
        /// 32-bit process.
        /// </summary>
        x32,

        /// <summary>
        /// 64-bit process.
        /// </summary>
        x64
    }
    /// <summary>
    /// Platform (operating system).
    /// </summary>
    internal enum Platform
    {
        /// <summary>
        /// Windows platform.
        /// </summary>
        Windows,

        /// <summary>
        /// Linux platform.
        /// </summary>
        Linux,

        /// <summary>
        /// MacOs platform.
        /// </summary>
        MacOs
    }
    /// <summary>
    /// A class to store the information about native library file.
    /// </summary>
    internal class LibraryFile
    {
        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="fileName">Filename to use when extracting the library.</param>
        /// <param name="resource">Library binary.</param>
        internal LibraryFile(string fileName, byte[] resource)
        {
            FileName = fileName;
            Resource = resource;
        }

        /// <summary>
        /// Filename to use when extracting the library.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Library binary.
        /// </summary>
        public byte[] Resource { get; set; }

        /// <summary>
        /// Specifies whether this file is a shared library, which can be loaded explicitly with
        /// <code>LoadLibraryEx</code> on Windows and <code>dlopen</code> on Linux and MacOs.
        ///
        /// Default is <code>True</code>, but explicit loading is disabled by default with
        /// <see cref="LibraryManager.LoadLibraryExplicit"/>.
        ///
        /// Set this to <code>False</code> if this file is not a library, but a supporting file which
        /// shouldn't be loaded explicitly when <see cref="LibraryManager.LoadLibraryExplicit"/> is <code>True</code>. 
        /// </summary>
        public bool CanLoadExplicitly { get; set; } = true;

        /// <summary>
        /// Gets the path to which current file will be unpacked.
        /// </summary>
        /// <param name="targetAssembly">Target assembly for which to compute the path.</param>
        [Obsolete("This method is no longer used to determine unpack path. It's determined at LibraryManager, once for all files.", true)]
        public string GetUnpackPath(Assembly targetAssembly)
        {
            return null;
        }
    }
    internal class LibraryItemInternal : LibraryItem
    {
        private readonly (Action<string> LogInformation, Action<string> LogWarning)? _logger;

        internal LibraryItemInternal(LibraryItem item, (Action<string> LogInformation, Action<string> LogWarning)? logger)
            : base(item.Platform, item.Bitness, item.Files)
        {
            _logger = logger;
        }

        public override void LoadItem(string targetDirectory, bool loadLibrary)
        {
            foreach (var file in Files)
            {
                string path = Path.Combine(targetDirectory, file.FileName);

                _logger?.LogInformation($"Unpacking native library {file.FileName} to {path}");

                UnpackFile(path, file.Resource);

                if (!loadLibrary || !file.CanLoadExplicitly)
                {
                    continue;
                }

                if (Platform == Platform.Windows)
                {
                    LoadWindowsLibrary(path);
                }
                else if (Platform == Platform.Linux || Platform == Platform.MacOs)
                {
                    LoadNixLibrary(path);
                }
            }
        }

        private void UnpackFile(string path, byte[] bytes)
        {
            if (File.Exists(path))
            {
                _logger?.LogInformation($"File {path} already exists, computing hashes.");
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(path))
                    {
                        string fileHash = BitConverter.ToString(md5.ComputeHash(stream));
                        string curHash = BitConverter.ToString(md5.ComputeHash(bytes));

                        if (string.Equals(fileHash, curHash))
                        {
                            _logger?.LogInformation($"Hashes are equal, no need to unpack.");
                            return;
                        }
                    }
                }
            }

            File.WriteAllBytes(path, bytes);
        }

        internal void LoadNixLibrary(string path)
        {
            _logger?.LogInformation($"Calling dlopen for {path}");
            var result = dlopen(path, RTLD_LAZY | RTLD_GLOBAL);
            _logger?.LogInformation(result == IntPtr.Zero ? "FAILED!" : "Success");
        }

        internal void LoadWindowsLibrary(string path)
        {
            _logger?.LogInformation($"Calling LoadLibraryEx for {path}...");
            var result = LoadLibraryEx(path, IntPtr.Zero, LoadLibraryFlags.LOAD_LIBRARY_SEARCH_APPLICATION_DIR | LoadLibraryFlags.LOAD_LIBRARY_SEARCH_DEFAULT_DIRS | LoadLibraryFlags.LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LoadLibraryFlags.LOAD_LIBRARY_SEARCH_SYSTEM32 | LoadLibraryFlags.LOAD_LIBRARY_SEARCH_USER_DIRS);
            _logger?.LogInformation(result == IntPtr.Zero ? "FAILED!" : "Success");
        }

        #region dlopen

        private const int RTLD_LAZY = 0x00001; //Only resolve symbols as needed
        private const int RTLD_GLOBAL = 0x00100; //Make symbols available to libraries loaded later
        [DllImport("dl")]
        private static extern IntPtr dlopen(string file, int mode);

        #endregion

        #region LoadLibraryEx

        [System.Flags]
        private enum LoadLibraryFlags : uint
        {
            DONT_RESOLVE_DLL_REFERENCES = 0x00000001,
            LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010,
            LOAD_LIBRARY_AS_DATAFILE = 0x00000002,
            LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040,
            LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020,
            LOAD_LIBRARY_SEARCH_APPLICATION_DIR = 0x00000200,
            LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000,
            LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR = 0x00000100,
            LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800,
            LOAD_LIBRARY_SEARCH_USER_DIRS = 0x00000400,
            LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, LoadLibraryFlags dwFlags);

        #endregion
    }
    /// <summary>
    /// A class to manage, extract and load native implementations of dependent libraries.
    /// </summary>
    internal class LibraryManager
    {
        private readonly object _resourceLocker = new object();
        private readonly LibraryItemInternal[] _items;
        private readonly (Action<string> LogInformation, Action<string> LogWarning)? _logger;

        private bool _libLoaded = false;

        /// <summary>
        /// Creates a new library manager which extracts to environment current directory by default.
        /// </summary>
        /// <param name="targetAssembly">Calling assembly.</param>
        /// <param name="items">Library binaries for different platforms.</param>
        [Obsolete("Specifying target assembly is no longer required since default target directory is environment current directory.")]
        public LibraryManager(Assembly targetAssembly, params LibraryItem[] items)
            : this(Environment.CurrentDirectory, false, null, items)
        {
        }

        /// <summary>
        /// Creates a new library manager which extracts to environment current directory by default.
        /// </summary>
        /// <param name="items">Library binaries for different platforms.</param>
        public LibraryManager(params LibraryItem[] items)
            : this(Environment.CurrentDirectory, false, null, items)
        {
        }

        /// <summary>
        /// Creates a new library manager which extracts to environment current directory by default.
        /// </summary>
        /// <param name="loggerFactory">Logger factory.</param>
        /// <param name="items">Library binaries for different platforms.</param>
        public LibraryManager(Func<Type, (Action<string> LogInformation, Action<string> LogWarning)> loggerFactory, params LibraryItem[] items)
            : this(Environment.CurrentDirectory, false, loggerFactory, items)
        {
        }

        /// <summary>
        /// Creates a new library manager which extracts to a custom directory.
        ///
        /// IMPORTANT! Be sure this directory is discoverable by system library loader. Otherwise, your library won't be loaded.
        /// </summary>
        /// <param name="targetDirectory">Target directory to extract the libraries.</param>
        /// <param name="loggerFactory">Logger factory.</param>
        /// <param name="items">Library binaries for different platforms.</param>
        public LibraryManager(string targetDirectory, Func<Type, (Action<string> LogInformation, Action<string> LogWarning)> loggerFactory, params LibraryItem[] items)
            : this(targetDirectory, true, loggerFactory, items)
        {
        }

        private LibraryManager(string targetDirectory, bool customDirectory, Func<Type, (Action<string> LogInformation, Action<string> LogWarning)> loggerFactory, params LibraryItem[] items)
        {
            TargetDirectory = targetDirectory;
            var itemLogger = loggerFactory?.Invoke(typeof(LibraryItem));

            _logger = loggerFactory?.Invoke(typeof(LibraryManager));
            _items = items.Select(x => new LibraryItemInternal(x, itemLogger)).ToArray();

            if (customDirectory)
            {
                _logger?.LogWarning("Custom directory for native libraries is specified. Be sure it is discoverable by system library loader.");
            }
        }

        /// <summary>
        /// Target directory to which native libraries will be extracted. Defaults to directory
        /// in which targetAssembly, passed to <see cref="LibraryManager"/> constructor, resides.
        /// </summary>
        public string TargetDirectory { get; }

        /// <summary>
        /// Defines whether shared libraries will be loaded explicitly. <code>LoadLibraryEx</code> is
        /// used on Windows and <code>dlopen</code> is used on Linux and MacOs to load libraries
        /// explicitly.
        ///
        /// WARNING! Explicit library loading on MacOs IS USELESS, and your P/Invoke call will fail unless
        /// library path is discoverable by system library loader.
        /// </summary>
        public bool LoadLibraryExplicit { get; set; } = false;

        /// <summary>
        /// Extract and load native library based on current platform and process bitness.
        /// Throws an exception if current platform is not supported.
        /// </summary>
        /// <param name="loadLibrary">
        /// Use LoadLibrary API call on Windows to explicitly load library into the process.
        /// </param>
        [Obsolete("This method is obsolete. Use LoadLibraryExplicit property.")]
        public void LoadNativeLibrary(bool loadLibrary)
        {
            LoadNativeLibrary();
        }

        /// <summary>
        /// Extract and load native library based on current platform and process bitness.
        /// Throws an exception if current platform is not supported.
        /// </summary>
        public void LoadNativeLibrary()
        {
            if (_libLoaded)
            {
                return;
            }

            lock (_resourceLocker)
            {
                if (_libLoaded)
                {
                    return;
                }

                var item = FindItem();

                if (item.Platform == Platform.MacOs && LoadLibraryExplicit)
                {
                    _logger?.LogWarning("Current platform is MacOs and LoadLibraryExplicit is specified. Explicit library loading on MacOs IS USELESS, and your P/Invoke call will fail unless library path is discoverable by system library loader.");
                }

                item.LoadItem(TargetDirectory, LoadLibraryExplicit);

                _libLoaded = true;
            }
        }

        /// <summary>
        /// Finds a library item based on current platform and bitness.
        /// </summary>
        /// <returns>Library item based on platform and bitness.</returns>
        /// <exception cref="NoBinaryForPlatformException"></exception>
        public LibraryItem FindItem()
        {
            var platform = GetPlatform();
            var bitness = Environment.Is64BitProcess ? Bitness.x64 : Bitness.x32;

            var item = _items.FirstOrDefault(x => x.Platform == platform && x.Bitness == bitness);
            if (item == null)
            {
                throw new NoBinaryForPlatformException($"There is no supported native library for platform '{platform}' and bitness '{bitness}'");
            }

            return item;
        }

        /// <summary>
        /// Gets the platform type.
        /// </summary>
        /// <exception cref="UnsupportedPlatformException">Thrown when platform is not supported.</exception>
        public static Platform GetPlatform()
        {
            string windir = Environment.GetEnvironmentVariable("windir");
            if (!string.IsNullOrEmpty(windir) && windir.Contains(@"\") && Directory.Exists(windir))
            {
                return Platform.Windows;
            }
            else if (File.Exists(@"/proc/sys/kernel/ostype"))
            {
                string osType = File.ReadAllText(@"/proc/sys/kernel/ostype");
                if (osType.StartsWith("Linux", StringComparison.OrdinalIgnoreCase))
                {
                    // Note: Android gets here too
                    return Platform.Linux;
                }
                else
                {
                    throw new UnsupportedPlatformException($"Unsupported OS: {osType}");
                }
            }
            else if (File.Exists(@"/System/Library/CoreServices/SystemVersion.plist"))
            {
                // Note: iOS gets here too
                return Platform.MacOs;
            }
            else
            {
                throw new UnsupportedPlatformException("Unsupported OS!");
            }
        }
    }
    /// <summary>
    /// Contains useful functions to get paths relative to target assembly.
    /// </summary>
    internal static class PathHelper
    {
        /// <summary>
        /// Gets the directory specified assembly is located in.
        /// If the assembly was loaded from memory, returns environment
        /// working directory.
        /// </summary>
        /// <param name="targetAssembly">Assembly to get the directory from.</param>
        public static string GetCurrentDirectory(this Assembly targetAssembly)
        {
            string curDir;
            var ass = targetAssembly.Location;
            if (string.IsNullOrEmpty(ass))
            {
                curDir = Environment.CurrentDirectory;
            }
            else
            {
                curDir = Path.GetDirectoryName(ass);
            }

            return curDir;
        }

        /// <summary>
        /// Combines part of the path with assembly's directory.
        /// </summary>
        /// <param name="targetAssembly">Assembly to get directory from.</param>
        /// <param name="fileName">Right-hand part of the path.</param>
        public static string CombineWithCurrentDirectory(this Assembly targetAssembly, string fileName)
        {
            string curDir = GetCurrentDirectory(targetAssembly);
            return !string.IsNullOrEmpty(curDir) ? Path.Combine(curDir, fileName) : fileName;
        }
    }
    /// <summary>
    /// Library binaries for specified platform and bitness.
    /// </summary>
    internal class LibraryItem
    {
        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="platform">Binary platform.</param>
        /// <param name="bitness">Binary bitness.</param>
        /// <param name="files">A collection of files for this bitness and platform.</param>
        public LibraryItem(Platform platform, Bitness bitness, params LibraryFile[] files)
        {
            Platform = platform;
            Bitness = bitness;
            Files = files;
        }

        /// <summary>
        /// Library files.
        /// </summary>
        public LibraryFile[] Files { get; set; }

        /// <summary>
        /// Platform for which this binary is used.
        /// </summary>
        public Platform Platform { get; set; }

        /// <summary>
        /// Bitness for which this binary is used.
        /// </summary>
        public Bitness Bitness { get; set; }

        [Obsolete("targetAssembly is no longer required. Use the other overload.")]
        public void LoadItem(Assembly targetAssembly, bool loadLibrary = true)
        {
        }

        /// <summary>
        /// Unpacks the library and directly loads it if on Windows.
        /// </summary>
        /// <param name="targetDirectory">Target directory to which library is extracted.</param>
        /// <param name="loadLibrary">Load library explicitly.</param>
        public virtual void LoadItem(string targetDirectory, bool loadLibrary)
        {
            throw new InvalidOperationException("This item was never added to the LibraryManager. Create a LibraryManager, add this item and then call LibraryManager.LoadNativeLibrary().");
        }
    }
    internal class NoBinaryForPlatformException : Exception
    {
        /// <inheritdoc />
        public NoBinaryForPlatformException()
        {
        }

        /// <inheritdoc />
        protected NoBinaryForPlatformException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <inheritdoc />
        public NoBinaryForPlatformException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public NoBinaryForPlatformException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Thrown when platform is not supported.
    /// </summary>
    internal class UnsupportedPlatformException : Exception
    {
        /// <inheritdoc />
        public UnsupportedPlatformException()
        {
        }

        /// <inheritdoc />
        protected UnsupportedPlatformException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <inheritdoc />
        public UnsupportedPlatformException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public UnsupportedPlatformException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
    /// <summary>
    /// A helper class to load resources from an assembly.
    /// </summary>
    internal class ResourceAccessor
    {
        private readonly Assembly _assembly;
        private readonly string _assemblyName;

        /// <summary>
        /// Creates a resource accessor for the specified assembly.
        /// </summary>
        public ResourceAccessor(Assembly assembly)
        {
            _assembly = assembly;
            _assemblyName = _assembly.GetName().Name;
        }

        /// <summary>
        /// Gets a resource with specified name as an array of bytes.
        /// </summary>
        /// <param name="name">Resource name with folders separated by dots.</param>
        /// <exception cref="InvalidOperationException">
        /// When resource is not found.
        /// </exception>
        public byte[] Binary(string name)
        {
            using (var stream = new MemoryStream())
            {
                var resource = _assembly.GetManifestResourceStream(GetName(name));
                if (resource == null)
                {
                    throw new InvalidOperationException("Resource not available.");
                }

                resource.CopyTo(stream);

                return stream.ToArray();
            }
        }

        private string GetName(string name) =>
            name.StartsWith(_assemblyName) ? name : $"{_assemblyName}.{name}";
    }
}


