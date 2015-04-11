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
    public static class extensions
    {
        public static T2 Pipe<T1, T2>(this T1 input, Func<T1, T2> fun)
        {
            return fun(input);
        }
    }
    class chipRead
    {
        public int bib { get; set; }
        public string gender { get; set; }
        public int age { get; set; }
        public int checkpoint { get; set; }
        public TimeSpan time { get; set; }
    }
    class Program
    {
        static string streamReaderUnfold(StreamReader sr)
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
        static IEnumerable<string> loadLines(Stream stream)
        {
            var sr = new StreamReader(stream);
            return Unfold(streamReaderUnfold, sr);
        }

        static IObservable<Tuple<string, chipRead>> groupReads(IObservable<chipRead> reads)
        {
            var overall = reads
                            .GroupBy(item => item.checkpoint)
                            .SelectMany(group => group
                                                    .Select(item => Tuple.Create(string.Format("Overall Checkpoint {0}", group.Key), item)));
            var genderGroup = reads
                            .GroupBy( item => Tuple.Create(item.checkpoint,item.gender))
                            .SelectMany( group => group
                                                    .Select(item=> {
                                                        var checkpoint = group.Key.Item1;
                                                        var gender = group.Key.Item2;
                                                        return Tuple.Create(string.Format("{0} Overall Checkpoint {1}",gender,checkpoint),item);
                                                    }));
            var genderAgeGroup = reads
                            .GroupBy( item => Tuple.Create(item.checkpoint,item.gender, (item.age+2)/5))
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

        static IObservable<Tuple<string, chipRead>> stream(IEnumerable<string> input)
        {
            return input
                    .Select(_ => JsonConvert.DeserializeObject<chipRead>(_))
                    .ToObservable(TaskPoolScheduler.Default)
                    .Publish()
                    .RefCount()
                    .Pipe(groupReads);
        }

        static void Main(string[] args)
        {
            var input = args.Length == 1 ? File.OpenRead(args[0]) : Console.OpenStandardInput();
            var lines = loadLines(input);

            var mre = new ManualResetEvent(false);
            var printer = stream(lines)
                            .Timeout(TimeSpan.FromMilliseconds(3000.0))
                            .Select(x => {
                                var group = x.Item1;
                                var item = x.Item2;
                                return new {groupName=group, bib=item.bib, time= item.time, age= item.age};
                            })
                            .Subscribe(item => Console.WriteLine("{0}", JsonConvert.SerializeObject(item)),
                                       err => Console.WriteLine("Error: {0}", err),
                                       () => mre.Set());
                            
            mre.WaitOne();
            printer.Dispose();
        }
    }
}
