﻿using System.IO;
using System.Reflection;
using Ninject;
using Tolltech.BayanMeterLib;
using Xunit;

namespace BayanMeterCoreTest
{
    public class ImageHashTest : TestBase
    {
        private readonly IImageHasher imageHasher;

        public ImageHashTest()
        {
            imageHasher = kernel.Get<IImageHasher>();
        }

        [Fact]
        public void TestGetHash()
        {
            var inputFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Images", "test.jpg");

            Assert.True(File.Exists(inputFilePath));

            var bytes = File.ReadAllBytes(inputFilePath);
            Assert.NotEmpty(bytes);

            var hash = imageHasher.GetHash(bytes);
            Assert.NotEmpty(hash);
        }
    }
}