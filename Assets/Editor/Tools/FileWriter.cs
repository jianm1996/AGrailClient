﻿using System;
using System.IO;
using System.Text;
using System.Collections;
namespace Editor
{
    public class FileWriter
    {
        FileStream fs;
        StreamWriter sw;
        public FileWriter(string fileName)
        {
            try
            {
                fs = new FileStream(fileName, FileMode.Create);
                fs.Seek(0, SeekOrigin.Begin);
                sw = new StreamWriter(fs, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                if (sw != null)
                    sw.Close();
                if (fs != null)
                    fs.Close();
            }
        }

        public void Append(string line)
        {
            try
            {
                fs.Seek(fs.Length, SeekOrigin.Begin);
                sw.WriteLine(line);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                if (sw != null)
                    sw.Close();
                if (fs != null)
                    fs.Close();
            }            
        }

        public void Dispose()
        {
            if (sw != null)
                sw.Close();
            if (fs != null)
                fs.Close();
        }
    }
}