using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Reflection;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using System.Text.RegularExpressions;

namespace RoboPiano
{
    public class Helper
    {
        #region parsing

        //конвертирует таблицу Lua в словарь C# (чтоб можно было уменьшить зависимсть кода от конкретного интерпретатора)
        public static Dictionary<string, object> CreateDictFromLuaTableRecursive(LuaInterface.LuaTable dict)
        {
            var res = new Dictionary<string, object>();
            foreach (var k in dict.Keys) // Loop through fields
            {
                var sk = Convert.ToString(k, CultureInfo.InvariantCulture);
                var val = dict[k];

                var val1 = val as LuaInterface.LuaTable;
                if (val1 != null) val = CreateDictFromLuaTableRecursive(val1);

                res[sk] = val;
            }
            return res;
        }

        //инициализирует структуру или класс по словарю с соответствующими ключами
        /*
         * пример:
                var p = new CarParams();
                var p_obj = (object)p;
                CarsShared.Helper.InitParamsFromLuaTable(p_obj, p0);
                p = (CarParams)p_obj;
         */
        public static void InitParamsFromLuaTable(object params_, LuaInterface.LuaTable dict)
        {
            var type = params_.GetType();
            foreach (var field in type.GetFields()) // Loop through fields
            {
                string name = field.Name; // Get string name                
                var v = dict[name];
                SetFieldValue(params_, field, Convert.ToString(v, CultureInfo.InvariantCulture));
            }
        }

        public static void SetFieldValue(object params_, FieldInfo field, string s)
        {
            var t = field.FieldType; // Get value
            if (t == typeof(int)) // See if it is an integer.
            {
                field.SetValue(params_, Helper.ParseInt(s));
            }
            else if (t == typeof(float))
            {
                field.SetValue(params_, Helper.ParseFloat(s));
            }
            else if (t == typeof(string))
            {
                field.SetValue(params_, s);
            }
            else if (t == typeof(bool))
            {
                field.SetValue(params_, Helper.ParseBool(s));
            }
        }

        public static float ParseFloat(string s)
        {
#warning magic conversion
            s = s.Replace(',', '.');
            var x = float.Parse(s, CultureInfo.InvariantCulture);
            return x;
        }

        public static int ParseInt(string s)
        {
            var x = int.Parse(s, CultureInfo.InvariantCulture);
            return x;
        }

        private static bool ParseBool(string s)
        {
            var x = s.ToLower();
            if (x == "1" || x == "true") return true;
            return false;
        }


        #endregion

        //переводит угол в диапазон (-turn/2; turn/2)
        public static void NormalizeAngle(ref float a, float turn)
        {
            float hturn = turn / 2;
            a %= turn;
            if (a < -hturn) a += turn;
            if (a > hturn) a -= turn;
        }

        //позволяет выполнять действие не на каждом кадре, а через указанные интервалы времени
        public static bool is_time(float T_calc, float dt, ref float t)
        {
            var time_to_calc = false;
            t += dt; if (t > T_calc) { time_to_calc = true; t %= T_calc; }
            return time_to_calc;
        }

        //архивирует укзанные файлы
        public static void ZipFiles(string[] in_paths, string out_path, bool add_time_prefix)
        {

            var prefix = String.Empty;
            if(add_time_prefix) prefix = DateTime.Now.ToString("[yyyy-MM-dd_HH-mm-ss]");

            // 'using' statements guarantee the stream is closed properly which is a big source
			// of problems otherwise.  Its exception safe as well which is great.
            using (ZipOutputStream s = new ZipOutputStream(File.Create(out_path)))
            {
				s.SetLevel(9); // 0 - store only to 9 - means best compression
		
				byte[] buffer = new byte[4096];

                foreach (string file in in_paths)
                {
					
					// Using GetFileName makes the result compatible with XP
					// as the resulting path is not absolute.
                    ZipEntry entry = new ZipEntry(prefix+Path.GetFileName(file));
					
					// Setup the entry data as required.
					
					// Crc and size are handled by the library for seakable streams
					// so no need to do them here.

					// Could also use the last write time or similar for the file.
					entry.DateTime = DateTime.Now;
					s.PutNextEntry(entry);
					
					using ( FileStream fs = File.OpenRead(file) ) {
		
						// Using a fixed size buffer here makes no noticeable difference for output
						// but keeps a lid on memory usage.
						int sourceBytes;
						do {
							sourceBytes = fs.Read(buffer, 0, buffer.Length);
							s.Write(buffer, 0, sourceBytes);
						} while ( sourceBytes > 0 );
					}
				}
				
				// Finish/Close arent needed strictly as the using statement does this automatically
				
				// Finish is important to ensure trailing information for a Zip file is appended.  Without this
				// the created file would be invalid.
				s.Finish();
				
				// Close is important to wrap things up and unlock the file.
				s.Close();
			}
        }

        public static string CorrectPath(string path)
        {
            var x = path.Replace("\\\\", "/").Replace("\\", "/");
            x = x.Replace("/", "\\");
            return x;
        }

        public static void UnZipFile(string in_path, string out_dir, bool remove_text_in_sq_brackets)
        {
            using (ZipInputStream s = new ZipInputStream(File.OpenRead(in_path)))
            {

                ZipEntry theEntry;
                while ((theEntry = s.GetNextEntry()) != null)
                {

                    Console.WriteLine(theEntry.Name);

                    string directoryName = out_dir+Path.GetDirectoryName(theEntry.Name)+"\\";
                    string fileName = Path.GetFileName(theEntry.Name);

                    if (remove_text_in_sq_brackets)
                    {
                        fileName = Regex.Replace(fileName, @" ?\[.*?\]", string.Empty);
                    }

                    // create directory
                    if (directoryName.Length > 0)
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    if (fileName != String.Empty)
                    {
                        var path = CorrectPath(directoryName + fileName);
                        using (FileStream streamWriter = File.Create(path))
                        {
                            int size = 2048;
                            byte[] data = new byte[2048];
                            while (true)
                            {
                                size = s.Read(data, 0, data.Length);
                                if (size > 0)
                                {
                                    streamWriter.Write(data, 0, size);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

    }
}
