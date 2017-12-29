using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SEIDR.Doc.Compression
{
    //ToDo: using PI is probably no good  without parallelism. 
    //Also would probably need millions of digits to really be effective.. Also mapping some other way probably..
    class PLZ : IDisposable
    {
        static int _b = 1000000;
        public static int BLOCK_SIZE
        {
            get { return _b; }
            set
            {
                if (value < 10000)
                    throw new ArgumentOutOfRangeException("BLOCK_SIZE", value, "Set Value is too small (Below 10000)");
                _b = value;
            }
        }

        static PLZ()
        {
            System.Diagnostics.Debug.WriteLine("STATIC SET UP PI");
            Window.SetPI(CalculatePI(15000));
            System.Diagnostics.Debug.WriteLine("FINISHED STATIC SET UP.");
        }
        public string Key
        {
            get { return _k; }
            set
            {
                _k = value;
                if (string.IsNullOrEmpty(value))
                    _k2 = 0;
                else
                {
                    int x = value[0];
                    foreach(char i in value)
                    {
                        x = x + i + (i % x);
                    }
                    _k2 = x;
                }
            }
        }
        string _k;
        int _k2;
        /// <summary>
        /// Taken from <see ref="stackoverflow.com/questions/11677369/how-to-calculate-pi-to-n-number-of-places-in-c-sharp-using-loops "/>
        /// </summary>
        /// <param name="digits"></param>
        /// <returns></returns>
        static string CalculatePI(int digits)
        {
            digits++;
            uint[] x = new uint[digits * 10 / 3 + 2];
            uint[] r = new uint[digits * 10 / 3 + 2];

            uint[] pi = new uint[digits];
            for (int j = 0; j < x.Length; j++)
                x[j] = 20;
            for(int i = 0; i< digits; i++)
            {
                uint carry = 0;
                for(int j= 0; j< x.Length; j++)
                {
                    uint num = (uint)(x.Length - j - 1);
                    uint dem = num * 2 + 1;
                    x[j] += carry;
                    uint q = x[j] / dem;
                    r[j] = x[j] % dem;
                    carry = q * num;
                }
                pi[i] = (x[x.Length - 1] / 10);
                r[x.Length - 1] = x[x.Length - 1] % 10;
                for (int j = 0; j < x.Length; j++)
                    x[j] = r[j] * 10;          
            }
            string result = "";
            uint c = 0;
            for(int i = pi.Length -1; i >= 0; i--)
            {
                pi[i] += c;
                c = pi[i] / 10;
                result = (pi[i] % 10).ToString() + result;
            }
            return result;
        }
        const string EXTENSION = ".PLZ";
        string _FilePath;
        string _Destination;
        Encoding _enc;
        WindowReader wr;
        StreamWriter sw;
        void Setup(string FilePath, string Destination, bool overwrite, Encoding enc, string key)
        {
            if (!File.Exists(FilePath))
                throw new ArgumentException("FilePath does not reference a valid file", "FilePath");

            if (enc == null)
                enc = Encoding.Default;
            _enc = enc;
            _FilePath = FilePath;
            if (string.IsNullOrWhiteSpace(Destination))
                _Destination = FilePath + EXTENSION;
            else
                _Destination = Destination;

            if (File.Exists(_Destination) && (overwrite || _Destination.GetFileSize() == 0)) 
                File.Delete(_Destination);
            else if (File.Exists(_Destination))
                throw new ArgumentException("Destination File already exists!", "Destination");
            else if (!Directory.Exists(Path.GetDirectoryName(_Destination)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_Destination));
            }

            wr = new WindowReader(FilePath, enc, BLOCK_SIZE);
            Key = key;
        }
        public PLZ(string FilePath, Encoding enc, 
            string Destination)
        {
            Setup(FilePath, Destination, false, enc, null);
        }
            
        public PLZ(string FilePath, Encoding enc, string Key, string Destination, bool Overwrite)            
        {
            Setup(FilePath, Destination, Overwrite, enc, Key);
        }
        public PLZ(string FilePath, Encoding enc = null, string Destination= null, bool Overwrite = false)            
        {
            Setup(FilePath, Destination, Overwrite, enc, null);
        }
        public void Compress()
        {
            if (wr == null)
                throw new ObjectDisposedException("PLZ");
            if (sw != null)
                sw.Dispose();
            sw = new StreamWriter(_Destination, false, _enc);
            string Header = wr.CreateHeader();
            wr.Reset();           
            sw.Write(Header);
            char[] map = Header.ToCharArray(1, Header.Length -1);
            Window work = null;
            string LeftOver = null;
            while(null != (work = wr.Read(LeftOver)))
            {
                work.SetKey(_k2);
                work.Compress(map);                
                LeftOver = work.LeftOver;
                sw.Write(work.Match);
                sw.Flush();
            }
            wr.Reset();
            sw.Close();        
            FAttModder.AddAttribute(_Destination, FileAttributes.Compressed);            
        }
        public void DeCompress()
        {
            if (wr == null)
                throw new ObjectDisposedException("PLZ");
            if (sw != null)
                sw.Dispose();
            char[] map = wr.ReadDecompressionHeader();
            wr.Reset();
            Window work = null;
            string LeftOver = null;
            while(null != (work = wr.Read(LeftOver)))
            {
                work.SetKey(_k2);
                work.Decompress(map);
                LeftOver = work.LeftOver;
                sw.Write(work.Match);
                sw.Flush();
            }
            wr.Reset();
            sw.Close();
        }        
        class WindowReader : IDisposable
        {
            readonly int BLOCK;
            StreamReader sw;
            int ALPHA = -1;
            public string DecompressionHeader { get; set; }
            public WindowReader(string FilePath, Encoding enc, int blockSize)
            {
                var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
                sw = new StreamReader(fs);
                BLOCK = blockSize;
            }
            
            public void Reset()
            {
                if (sw == null)
                    throw new ObjectDisposedException("WindowReader");
                sw.BaseStream.Flush();
                sw.BaseStream.Seek(0, SeekOrigin.Begin);                
            }
            
            public string CreateHeader()
            {
                if (sw == null)
                    throw new ObjectDisposedException("WindowReader");                
                List<char> temp = new List<char>();
                Dictionary<char, uint> Extras = new Dictionary<char, uint>();
                var work = new char[BLOCK];
                int x = BLOCK;
                while (x > 0)
                {
                    x = sw.ReadBlock(work, 0, BLOCK);
                    string temp0 = new string(work, 0, x);
                    temp0.ForEach(t =>
                    {
                        if (Extras.ContainsKey(t))
                            Extras[t]++;
                        else
                            Extras[t] = 0;
                    });
                    temp.AddRange(temp0.Except(temp).Distinct());
                }
                int range = (int)Math.Pow(Math.Log(temp.Count, 2), Math.Log(temp.Count, 2) + 1) - temp.Count;
                var ExtraKV = Extras.OrderByDescending(kv => kv.Value);
                ExtraKV.ForEach( kv => temp.Add(kv.Key), range);

                Reset();
                ALPHA = temp.Count;
                if (ALPHA == 0)
                    throw new Exception("File Has no content");
                string r = (char)ALPHA + new string(temp.ToArray());
                return r;
            }
            public char[] ReadDecompressionHeader()
            {
                if (sw == null)
                    throw new ObjectDisposedException("WindowReader");
                //Number of characters + List of the characters... dictionary for mapping..
                int x = BLOCK;
                if (x < char.MaxValue + 2)
                    x = char.MaxValue + 2; //Should be bigger than the value that will be in work[0]

                char[] work = new char[x];
                int len = sw.ReadBlock(work, 0, x);

                if (len <= work[0] || sw.EndOfStream)
                    throw new Exception("Invalid File");

                Reset();
                char[] Mapping = new char[work[0]];
                for(int i = 1; i<= work[0]; i++)
                {
                    Mapping[i - 1] = work[i];
                }
                return Mapping;
            }
            public Window Read(string LeftOver)
            {
                if (sw == null)
                    throw new ObjectDisposedException("WindowReader");
                if (ALPHA <= 0)
                    throw new InvalidOperationException("WindowReader Setup is incomplete");
                if (LeftOver == null)
                    LeftOver = string.Empty;
                if (LeftOver.Length > 0)
                {
                    if (LeftOver.Length >= BLOCK)
                        return new Window(LeftOver, ALPHA, BLOCK);
                    if (sw.EndOfStream)
                        return new Window(LeftOver, ALPHA, BLOCK);
                }

                var work = new char[BLOCK];
                int x = sw.ReadBlock(work, 0, BLOCK);
                if (x == 0 && LeftOver.Length == 0)
                    return null;
                else if (x == 0)
                    return new Window(LeftOver, ALPHA, BLOCK);

                string s = LeftOver + new string(work, 0, x);
                Window w = new Window(s, ALPHA, BLOCK);
                return w;
            }
            #region dispose
            ~WindowReader()
            {
                Dispose();
            }
            public void Dispose()
            {
                if (sw == null)
                    return;
                ((IDisposable)sw).Dispose();
                sw = null;
                GC.SuppressFinalize(this);
            }
            #endregion
        }
        class Window
        {            
            public static void SetPI(string val)
            {
                PI = val;                
            }
            static string PI;            
            public Window(string content, int Alpha, int GOAL)
            {
                Content = content;
                Alphabet = Alpha;
                if (content.Length < GOAL)
                    TempGoal = content.Length;
                else
                    TempGoal = GOAL;
            }
            public void SetKey(int Key)
            {
                _k = Key % PI.Length;
            }
            int _A;
            int Alphabet { get
                {
                    return _A;
                }
                set
                {
                    _A = value;
                    SubLength = Math.Pow(2, ((int)Math.Log(value, 2) + 1)).ToString().Length;
                }
            }
            string Content;            
            public string Match { get; private set; }      
            public string LeftOver
            {
                get;private set;
            }
            int _k;   
            public int Offset { get; private set; }
            public int MatchLength  { get; private set;}
            int CurrentOffset;
            int CurrentMatch;
            int TempGoal;            
            string GetMappingCheck(int offset)
            {
                return PI.Substring(offset % PI.Length, SubLength);
            }
            int SubLength;
            public void Compress(char[] alpha)
            {
                char[] temp = new char[alpha.Length * 2];
                alpha.ForEachIndex((a, i) => { temp[i] = a; temp[i + alpha.Length] = a; }, 0, 1);
                alpha = temp;
                Alphabet = alpha.Length;
                CurrentMatch = 0;
                CurrentOffset = _k;
                MatchLength = Offset = -1;

                while(CurrentMatch < TempGoal)
                {
                    //If this actually does work well for compression, could multithread pretty easily..
                    int idx = CurrentOffset + (CurrentMatch * SubLength) + SubLength;
                    int tidx = idx % PI.Length;
                    if (tidx > SubLength)
                        tidx -= SubLength;
                    if(Map(Content[CurrentMatch],
                        //GetMappingCheck(CurrentOffset + CurrentMatch),
                        PI.Substring(tidx, SubLength),
                        alpha) >= 0)
                    {
                        CurrentMatch++; 
                        if(CurrentMatch == TempGoal)
                        {
                            MatchLength = CurrentMatch;
                            Offset = CurrentOffset - _k;
                            break;
                        }
                    }
                    else
                    {
                        if (CurrentMatch > MatchLength)
                        {
                            MatchLength = CurrentMatch;
                            Offset = CurrentOffset - _k;                            
                        }
                        CurrentMatch = 0;
                        CurrentOffset++;
                        if (CurrentOffset >= char.MaxValue)
                            break;// limit number of failures/Size of offset
                    }
                }
                if (Offset < 0)
                    throw new Exception("Could not Compress segment! Try setting a key.");
                //Match = Content.Substring(0, MatchLength); //Need to change into getting compressed content.
                //int right = MatchLength ^ _O;
                Match = new string(
                    new char[] {
                        (char) (Offset /*<< _S + right*/),
                        (char) (MatchLength /*- right*/)
                    });
                if (MatchLength == Content.Length)
                    LeftOver = null;
                else
                    LeftOver = Content.Substring(MatchLength); 
            }
            const int _S = 4; //sizeof(char) / 2;
            const int _O = int.MaxValue >> _S;
            //const int _O = 2 * sizeof(char) - _S;
            public void Decompress(char[] map)
            {
                char[] temp = new char[map.Length * 2];
                map.ForEachIndex((a, idx) => { temp[idx] = a; temp[idx + map.Length] = a; });
                map = temp;
                Offset = MatchLength = CurrentOffset = CurrentMatch = 0;
                MatchLength = CurrentMatch = 0;
                CurrentOffset = _k;
                Offset = -1;
                StringBuilder sb = new StringBuilder();
                int i = 0;
                for (; i < Content.Length; i += 2 /* Unmap size...*/)
                {
                    Offset = Content[i];// >> _S;
                    MatchLength = /*Content[i] << _S >> _S << _S +*/ Content[i + 1];
                    for(int j = 0; j < MatchLength; j++)
                    {
                        int idx = (j *SubLength) + Offset + _k + SubLength;
                        int tidx = idx % PI.Length;
                        if (tidx > SubLength)
                            tidx -= SubLength;
                        sb.Append(
                            map[
                                UnMap(PI.Substring(tidx, MatchLength), SubLength)
                                ]);
                    }
                }
                Match = sb.ToString();
                if (i > Content.Length)
                    LeftOver = null;
                else
                    LeftOver = Content.Substring(i);
            }
            int UnMap(string pi, int Alphabet)
            {
                return int.Parse(pi) % Alphabet;
            }
            int Map(char v, string pi, char[] dest)
            {
                int x = int.Parse(pi) % dest.Length;
                if (dest[x] != v)
                    return -1;
                return x;
            }
            //public Func<string, int> UnMap { get; set; }
            //Should get an array index using the string.
            //public Func<char, string, char[], int> Map { get; set; } 
            //Should map the character to its position in the char array based on the value of the string
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }
                wr.Dispose();
                sw.Dispose();
                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~PLZ()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
