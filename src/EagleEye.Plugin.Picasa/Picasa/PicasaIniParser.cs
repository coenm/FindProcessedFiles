﻿namespace EagleEye.Picasa.Picasa
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using EagleEye.Picasa.IniParser;

    public static class PicasaIniParser
    {
        private const string Contacts2Section = "Contacts2";
        private const string FacesKey = "faces";
        private static readonly string[] CoordinatePersonSeparator = { "," };

        public static IEnumerable<FileWithPersons> Parse(Stream stream)
        {
            var iniContent = SimpleIniParser.Parse(stream);

            var contacts = iniContent.SingleOrDefault(x => x.Section == Contacts2Section);
            if (contacts == null)
                throw new Exception($"{Contacts2Section} not found");

            var result = new List<FileWithPersons>(iniContent.Count - 1);

            foreach (var item in iniContent.Where(x => x != contacts))
            {
                var fileWithPersons = new FileWithPersons(item.Section);
                var facesList = item.Content.Where(x => x.Key == FacesKey).ToList();
                if (facesList.Count == 1)
                {
                    var facesString = facesList.Single().Value;

                    // rect64(9ee42f2ee2e49bfa),4759b81b11610b7a;rect64(9ee42f2ee2e49bfa),4759b81b11610b7a
                    // first split on ';'
                    var facesCoordinateKey = facesString.Split(';');
                    foreach (var faceCoordinateKey in facesCoordinateKey)
                    {
                        // like: rect64(9ee42f2ee2e49bfa),4759b81b11610b7a
                        // means: <coordinate>,<person key>
                        var singleCoordinateAndKey = faceCoordinateKey.Split(CoordinatePersonSeparator, StringSplitOptions.RemoveEmptyEntries);

                        // expect only two items
                        if (singleCoordinateAndKey.Length != 2)
                            continue;

                        var coordinate = GetRelativeCoordinates(singleCoordinateAndKey[0]);
                        var personName = GetName(singleCoordinateAndKey[1], contacts);
                        if (!string.IsNullOrWhiteSpace(personName))
                            fileWithPersons.AddPerson(new PicasaPerson(personName, coordinate));
                    }
                }

                result.Add(fileWithPersons);
            }

            return result;
        }

        private static RelativeRegion GetRelativeCoordinates(string rect64)
        {
            const int expectedLength = 7 + 16 + 1;
            if (rect64 == null)
                return null;
            if (rect64.Length != expectedLength)
                return null;
            if (!rect64.StartsWith("rect64("))
                return null;
            if (!rect64.EndsWith(")"))
                return null;

            var left = FromString(rect64, 0 + 7);
            var top = FromString(rect64, 4 + 7);
            var right = FromString(rect64, 8 + 7);
            var bottom = FromString(rect64, 12 + 7);

            var relativeLeft = (float)left / (float)ushort.MaxValue;
            var relativeTop = (float)top / (float)ushort.MaxValue;
            var relativeRight = (float)right / (float)ushort.MaxValue;
            var relativeBottom = (float)bottom / (float)ushort.MaxValue;

            return new RelativeRegion(relativeLeft, relativeTop, relativeRight, relativeBottom);
        }

        private static ushort FromString(string input, int startIndex)
        {
            var bytes = StringToByteArray(input.Substring(startIndex, 4));

            if (BitConverter.IsLittleEndian)
                bytes = bytes.Reverse().ToArray();

            return BitConverter.ToUInt16(bytes, 0);
        }

        private static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }

        private static string GetName(string key, IniData contacts)
        {
            if (!contacts.Content.ContainsKey(key))
                return string.Empty;

            var contact = contacts.Content[key];

            while (contact.EndsWith(";"))
                contact = contact.Substring(0, contact.Length - 1);

            return contact;
        }
    }
}
