using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Flexinets.Ldap.Core.Tests
{
    public class BerLengthTests
    {     
        [TestCase]
        public void TestBerLengthLongNotation5()
        {
            var bytes = Utils.StringToByteArray("300c02010161070a010004000400");
            var position = 1;
            var intLength = Utils.BerLengthToInt(bytes, 1, out position);

            Assert.AreEqual(12, intLength);
        }


        [TestCase]
        public void TestBerLengthLongNotation6()
        {
            var bytes = Utils.StringToByteArray("300c02010161070a010004000400");
            var position = 1;
            var intLength = Utils.BerLengthToInt(bytes, 3, out position);

            Assert.AreEqual(1, intLength);
        }





        [TestCase("8400000159", 345)]
        [TestCase("840000014f", 335)]
        [TestCase("840000012b", 299)]
        public void TestBerToInt(string input, int expected)
        {
            var position = 0;
            var intlength = Utils.BerLengthToInt(Utils.StringToByteArray(input), 0, out position);

            Assert.AreEqual(expected, intlength);
        }

        [TestCase("8400000159", 345)]
        [TestCase("840000014f", 335)]
        [TestCase("840000012b", 299)]
        public async Task TestBerToIntAsync(string input, int expected)
        {
            var bytes = Utils.StringToByteArray(input);
            var stream = new MemoryStream(bytes, 0, bytes.Length, false);

            var result = await Utils.BerLengthToIntAsync(stream);

            Assert.AreEqual(expected, result.Length);
        }


        [TestCase(1, "01")]        
        [TestCase(127, "7f")]
        [TestCase(128, "8400000080")]
        [TestCase(345, "8400000159")]
        [TestCase(335, "840000014f")]
        [TestCase(299, "840000012b")]
        public void TestIntToBer(int input, string expected)
        {
            var intlength = Utils.ByteArrayToString(Utils.IntToBerLength(input));

            Assert.AreEqual(expected, intlength);
        }
    }
}
