﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Redwood.Framework.Parser;

namespace Redwood.Framework.Hosting
{
    public class MarkupFile
    {
        protected bool Equals(MarkupFile other)
        {
            return string.Equals(FullPath, other.FullPath, StringComparison.CurrentCultureIgnoreCase) && LastWriteDateTimeUtc.Equals(other.LastWriteDateTimeUtc);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((FullPath != null ? FullPath.ToLower().GetHashCode() : 0) * 397) ^ LastWriteDateTimeUtc.GetHashCode();
            }
        }

        public const string ViewFileExtension = ".rwhtml";



        public Func<IReader> ContentsReaderFactory { get; private set; }

        public string FileName { get; private set; }

        public string FullPath { get; private set; }

        public DateTime LastWriteDateTimeUtc { get; private set; }


        public MarkupFile(string fileName, string fullPath)
        {
            FileName = fileName;
            FullPath = fullPath;
            LastWriteDateTimeUtc = File.GetLastWriteTimeUtc(fullPath);
            ContentsReaderFactory = () => new FileReader(fullPath);
        }

        internal MarkupFile(string fileName, string fullPath, string contents)
        {
            FileName = fileName;
            FullPath = fullPath;
            ContentsReaderFactory = () => new Parser.StringReader(contents);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((MarkupFile)obj);
        }
    }
}