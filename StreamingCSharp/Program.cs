using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Linq;
using Newtonsoft.Json;
using System.Reactive.Concurrency;
namespace StreamingCSharp
{
    public static class Extensions
    {
        public static T2 Pipe<T1, T2>(this T1 input, Func<T1, T2> fun)
        {
            return fun(input);
        }
    }
    class ChipRead
    {
        public int Bib { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public int Checkpoint { get; set; }
        public TimeSpan Time { get; set; }
    }
    class Program
    {
        static string StreamReaderUnfold(StreamReader sr)
        {
            var str = sr.ReadLine();
            if (str == null) { sr.Dispose(); return null; }
            if (str.Length == 0) { sr.Dispose(); return null; }
            if (str.Length == 1 && Char.IsControl(str[0])) { sr.Dispose(); return null; }
            return str;
        }
        static IEnumerable<T2> Unfold<T1, T2>(Func<T1, T2> unfolder, T1 state)
        {
            do
            {
                var res = unfolder(state);
                if (res == null)
                {
                    yield break;
                }
                yield return res;
            } while (true);
        }
        static IEnumerable<string> LoadLines(Stream stream)
        {
            var sr = new StreamReader(stream);
            return Unfold(StreamReaderUnfold, sr);
        }

        static IObservable<Tuple<string, ChipRead>> GroupReads(IObservable<ChipRead> reads)
        {
            var overall = reads
                            .GroupBy(item => item.Checkpoint)
                            .SelectMany(group => group
                                                    .Select(item => Tuple.Create(string.Format("Overall Checkpoint {0}", group.Key), item)));
            var genderGroup = reads
                            .GroupBy( item => Tuple.Create(item.Checkpoint,item.Gender))
                            .SelectMany( group => group
                                                    .Select(item=> {
                                                        var checkpoint = group.Key.Item1;
                                                        var gender = group.Key.Item2;
                                                        return Tuple.Create(string.Format("{0} Checkpoint {1}",gender,checkpoint),item);
                                                    }));
            var genderAgeGroup = reads
                            .GroupBy( item => Tuple.Create(item.Checkpoint,item.Gender, (item.Age+2)/5))
                            .SelectMany( group => group
                                                    .Select(item=> {
                                                        var checkpoint = group.Key.Item1;
                                                        var gender = group.Key.Item2;
                                                        var ageGroup = group.Key.Item3;
                                                        var ageRange = string.Format( "{0}-{1}", ageGroup * 5 - 2, (ageGroup + 1) * 5 - 2);
                                                        return Tuple.Create(string.Format("{0} {1} Checkpoint {2}",gender,ageRange,checkpoint),item);
                                                    }));
            return overall
                    .Merge(genderGroup)
                    .Merge(genderAgeGroup);
        }

        static IObservable<Tuple<string, ChipRead>> Stream(IEnumerable<string> input)
        {
            return input
                    .Select(_ => JsonConvert.DeserializeObject<ChipRead>(_))
                    .ToObservable(TaskPoolScheduler.Default)
                    .Publish()
                    .RefCount()
                    .Pipe(GroupReads);
        }

        static void Main(string[] args)
        {
            var input = args.Length == 1 ? File.OpenRead(args[0]) : Console.OpenStandardInput();
            var lines = LoadLines(input);

            var mre = new ManualResetEvent(false);
            var printer = Stream(lines)
                            .Timeout(TimeSpan.FromMilliseconds(3000.0))
                            .Select(x => {
                                var group = x.Item1;
                                var item = x.Item2;
                                return new {groupName=group, bib=item.Bib, time= item.Time, age= item.Age};
                            })
                            .Subscribe(item => Console.WriteLine("{0}", JsonConvert.SerializeObject(item)),
                                       err => Console.WriteLine("Error: {0}", err),
                                       () => mre.Set());
                            
            mre.WaitOne();
            printer.Dispose();
        }
    }
}
