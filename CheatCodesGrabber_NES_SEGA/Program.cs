using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

internal class Program
{
    private static string fileSave = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_grab_data.html");

    private static void Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        WriteError("", true);
        WriteOut("Cheat Code Grabber NES/SEGA by dkxce v1.0", true);
        WriteOut();

        Console.WriteLine("Cheat Code Grabber NES/SEGA by dkxce v1.0");
        Console.WriteLine();

        Run();
    }

    private static void Run()
    {
        GrabEL("https://www.emu-land.net/consoles/genesis/cheats", "https:", "emu-land.net\\Sega");
        GrabEL("https://www.emu-land.net/consoles/dendy/cheats", "https:", "emu-land.net\\Nes");
        GrabER("http://emulations.ru/cheats.php?id=6", "http://emulations.ru/", "emulations.ru\\Sega");
        GrabER("http://emulations.ru/cheats.php?id=2", "http://emulations.ru/", "emulations.ru\\Nes");
        GrabPSCD("http://pscd.ru/codes/smdcodes/page/{page}/", 1, 5, "pscd.ru\\Sega");
        GrabPSCD("http://pscd.ru/codes/nescodes/page/{page}/", 1, 35, "pscd.ru\\Nes");
        WriteOut("Done");
        Console.WriteLine("Done");
        try { if (File.Exists(fileSave)) File.Delete(fileSave); } catch { };
        Console.ReadLine();
    }

    private static void WriteOut(string text = "", bool clear = false, bool newline = true)
    {
        byte[] data = Encoding.GetEncoding(1251).GetBytes(text + (newline ? "\r\n" : ""));

        string fName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_grab_data.log");
        if (clear && File.Exists(fName)) try { File.Delete(fName); } catch { };
        FileStream fs = null;
        try
        {
            fs = new FileStream(fName, FileMode.Append, FileAccess.Write);
            fs.Write(data, 0, data.Length);
        }
        catch { }
        finally { if (fs != null) fs.Close(); };
    }

    private static void WriteError(string text = "", bool clear = false)
    {
        byte[] data = Encoding.GetEncoding(1251).GetBytes(text + "\r\n");

        string fName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_grab_errors.log");
        if (clear && File.Exists(fName)) try { File.Delete(fName); } catch { };
        if (string.IsNullOrEmpty(text)) return;
        FileStream fs = null;
        try
        {
            fs = new FileStream(fName, FileMode.Append, FileAccess.Write);
            fs.Write(data, 0, data.Length);
        }
        catch { }
        finally { if (fs != null) fs.Close(); };
    }

    private static void GrabFile(string url)
    {
        try
        {
            using (WebClient client = new WebClient())
                client.DownloadFile(url, fileSave);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            WriteOut($"Error: {ex.Message}");
        };
    }

    private static string FindText(string sText, string eText, Encoding enc)
    {
        string txt = "";
        try
        {
            using (var reader = new StreamReader(fileSave, enc))
                txt = reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            WriteOut($"Error: {ex.Message}");
            Console.WriteLine($"Error: {ex.Message}");
            return "";
        }
        int iof = txt.IndexOf(sText);
        if (iof < 0) return "";
        int die = txt.IndexOf(eText, iof) + 6;
        if (die < iof) return "";
        return txt.Substring(iof, die - iof);
    }

    private static Dictionary<string, string> GetUrls(string text)
    {
        const string PATTERN = @"(<a.*?>.*?</a>)";
        MatchCollection collection = Regex.Matches(text, PATTERN, RegexOptions.Singleline);
        Dictionary<string, string> res = new Dictionary<string, string>();
        foreach (Match match in collection)
        {
            string a = match.Groups[1].Value;
            string href = "";
            Match m2 = Regex.Match(a, @"href=\""(.*?)\""", RegexOptions.Singleline);
            if (m2.Success)
                href = m2.Groups[1].Value;
            else
            {
                m2 = Regex.Match(a, @"href='(.*?)'", RegexOptions.Singleline);
                if (m2.Success)
                    href = m2.Groups[1].Value;
                else
                    continue;
            };
            string linkText = Regex.Replace(a, @"\s*<.*?>\s*", "", RegexOptions.Singleline); //= Microsoft
            try { res.Add(linkText, href); } catch { };
        };
        return res;
    }

    private static string PrepareName(string name)
    {
        return name.Replace(":", "-").Replace("/", "-").Replace("\\", "-").Replace("?", "").Replace("\"", "") + ".txt";
    }

    private static void GrabEL(string url /* emu-land.net */, string prefix, string dir)
    {
        string html = "";
        int cur = 0;
        int ttl = 0;

        // Step 1
        Console.WriteLine($"Grab {url} as {dir}");
        WriteOut($"Grab {url} as {dir}");
        GrabFile(url);
        html = FindText("<div class=\"pagelist\"", "</div>", System.Text.Encoding.UTF8);
        Dictionary<string, string> urls = GetUrls(html);
        Console.WriteLine($" Found: {urls.Count} pages");
        WriteOut($" Found: {urls.Count} pages");
        ttl += urls.Count;

        foreach (KeyValuePair<string, string> kvp in urls)
        {
            url = prefix + kvp.Value;
            cur++;

            // Step 2
            Console.WriteLine($"  {cur}/{ttl} Grab {url} as {Path.Combine(dir, kvp.Key)}");
            WriteOut($"  {cur}/{ttl} Grab {url} as {Path.Combine(dir, kvp.Key)}");
            GrabFile(url);
            html = FindText("<div class=\"fllinks\">", "</div>", System.Text.Encoding.UTF8);
            Dictionary<string, string> subUrls = GetUrls(html);
            Console.WriteLine($"   Found: {subUrls.Count} pages");
            WriteOut($"   Found: {subUrls.Count} pages");
            ttl += subUrls.Count;

            foreach (KeyValuePair<string, string> kvp2 in subUrls)
            {
                string cUrl = prefix + kvp2.Value;
                string sfn = PrepareName(kvp2.Key);
                cur++;

                // Step 3
                Console.Write($"    {cur}/{ttl} Save {sfn} - ", false, false);
                WriteOut($"    {cur}/{ttl} Save {cUrl.Substring(cUrl.Length - 5)} as {sfn} - ", false, false);
                GrabFile(cUrl);
                html = FindText("<div class=\"fheader\">", "<div class=\"bcontent\">", System.Text.Encoding.UTF8);

                if (string.IsNullOrEmpty(html))
                {
                    Console.WriteLine($"No Text");
                    WriteOut($"No Text");
                }
                else
                {
                    html = HtmlToPlainText(html);
                    html += "\r\n\r\n" + cUrl;
                    string dn = Path.Combine(Path.GetDirectoryName(fileSave), dir, kvp.Key);

                    try
                    {
                        Directory.CreateDirectory(dn);
                        sfn = Path.Combine(dn, sfn);
                        File.WriteAllText(sfn, html, Encoding.GetEncoding(1251));
                        Console.WriteLine("OK");
                        WriteOut("OK");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        WriteOut($"Error: {ex.Message}");
                        WriteError($"Error: {ex.Message} as {url}");
                    };
                };
            };
        };
    }

    private static void GrabER(string url /* emulations.ru */, string prefix, string dir)
    {
        string html = "";
        int cur = 0;
        int ttl = 0;

        // Step 1
        Console.WriteLine($"Grab {url} as {dir}");
        WriteOut($"Grab {url} as {dir}");
        GrabFile(url);
        html = FindText("Выберите букву, на которую начинается название игры:", "</div>", System.Text.Encoding.GetEncoding(1251));
        Dictionary<string, string> urls = GetUrls(html);
        Console.WriteLine($" Found: {urls.Count} pages");
        WriteOut($" Found: {urls.Count} pages");
        ttl += urls.Count;

        foreach (KeyValuePair<string, string> kvp in urls)
        {
            url = prefix + kvp.Value;
            cur++;

            // Step 2
            Console.WriteLine($"  {cur}/{ttl} Grab {url} as {Path.Combine(dir, kvp.Key)}");
            WriteOut($"  {cur}/{ttl} Grab {url} as {Path.Combine(dir, kvp.Key)}");
            GrabFile(url);
            html = FindText("<ul><li>", "</li></ul>", System.Text.Encoding.GetEncoding(1251));
            Dictionary<string, string> subUrls = GetUrls(html);
            Console.WriteLine($"   Found: {subUrls.Count} pages");
            WriteOut($"   Found: {subUrls.Count} pages");
            ttl += subUrls.Count;

            foreach (KeyValuePair<string, string> kvp2 in subUrls)
            {
                string cUrl = prefix + kvp2.Value;
                string sfn = PrepareName(kvp2.Key);
                cur++;

                // Step 3
                Console.Write($"    {cur}/{ttl} Save {sfn} - ", false, false);
                WriteOut($"    {cur}/{ttl} Save {cUrl.Substring(cUrl.Length - 5)} as {sfn} - ", false, false);
                GrabFile(cUrl);
                html = FindText("Коды, пароли, секреты к", "<!-- Yandex.RTB R-A-19571-5 -->", System.Text.Encoding.GetEncoding(1251));

                if (string.IsNullOrEmpty(html))
                {
                    Console.WriteLine($"No Text");
                    WriteOut($"No Text");
                }
                else
                {
                    html = HtmlToPlainText(html);
                    html += "\r\n\r\n" + cUrl;
                    string dn = Path.Combine(Path.GetDirectoryName(fileSave), dir, kvp.Key);

                    try
                    {
                        Directory.CreateDirectory(dn);
                        sfn = Path.Combine(dn, sfn);
                        File.WriteAllText(sfn, html, Encoding.GetEncoding(1251));
                        Console.WriteLine("OK");
                        WriteOut("OK");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        WriteOut($"Error: {ex.Message}");
                        WriteError($"Error: {ex.Message} as {url}");
                    };
                };
            };
        };
    }

    private static void GrabPSCD(string url /* pscd.ru */, int pageFrom, int pageTo, string dir)
    {
        string html = "";
        int cur = 0;
        int ttl = 0;
        
        Console.WriteLine($"Grab {url} as {dir}");
        WriteOut($"Grab {url} as {dir}");

        for (int i = pageFrom; i <= pageTo; i++)
        {
            string cUrl = url.Replace("{page}", i.ToString());

            // Step 1
            Console.WriteLine($" Grab {cUrl} as {dir}");
            WriteOut($" Grab {cUrl} as {dir}");
            GrabFile(cUrl);
            html = FindText("<div class=\"newstitle\">", "<span class=\"navigation\">", System.Text.Encoding.GetEncoding(1251));
            Dictionary<string, string> urls = GetUrls(html);
            Console.WriteLine($"  Found: {urls.Count} pages");
            WriteOut($"  Found: {urls.Count} pages");
            ttl += urls.Count;

            foreach (KeyValuePair<string, string> kvp2 in urls)
            {
                cUrl = kvp2.Value;
                string sfn = PrepareName(kvp2.Key);
                cur++;

                // Step 3
                Console.Write($"    {cur}/{ttl} Save {sfn} - ", false, false);
                WriteOut($"    {cur}/{ttl} Save {cUrl.Substring(cUrl.Length - 5)} as {sfn} - ", false, false);
                GrabFile(cUrl);
                html = FindText("<div class=\"maincont\">", "<div class=\"clr\"></div>", System.Text.Encoding.GetEncoding(1251));

                if (string.IsNullOrEmpty(html))
                {
                    Console.WriteLine($"No Text");
                    WriteOut($"No Text");
                }
                else
                {
                    html = HtmlToPlainText(html);
                    html += "\r\n\r\n" + cUrl;
                    string dn = Path.Combine(Path.GetDirectoryName(fileSave), dir);

                    try
                    {
                        Directory.CreateDirectory(dn);
                        sfn = Path.Combine(dn, sfn);
                        File.WriteAllText(sfn, html, Encoding.GetEncoding(1251));
                        Console.WriteLine("OK");
                        WriteOut("OK");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        WriteOut($"Error: {ex.Message}");
                        WriteError($"Error: {ex.Message} as {url}");
                    };
                };
            };
        };                    
    }

    private static string HtmlToPlainText(string html)
    {
        const string tagWhiteSpace = @"(>|$)(\W|\n|\r)+<";//matches one or more (white space or line breaks) between '>' and '<'
        const string stripFormatting = @"<[^>]*(>|$)";//match any character between '<' and '>', even when end tag is missing
        const string lineBreak = @"<(br|BR)\s{0,1}\/{0,1}>";//matches: <br>,<br/>,<br />,<BR>,<BR/>,<BR />
        Regex lineBreakRegex = new Regex(lineBreak, RegexOptions.Multiline);
        Regex stripFormattingRegex = new Regex(stripFormatting, RegexOptions.Multiline);
        Regex tagWhiteSpaceRegex = new Regex(tagWhiteSpace, RegexOptions.Multiline);

        string text = html;        
        text = System.Net.WebUtility.HtmlDecode(text); //Decode html specific characters        
        text = tagWhiteSpaceRegex.Replace(text, "><"); //Remove tag whitespace/line breaks        
        text = lineBreakRegex.Replace(text, Environment.NewLine); //Replace <br /> with line breaks        
        text = stripFormattingRegex.Replace(text, string.Empty); //Strip formatting

        return text;
    }
}