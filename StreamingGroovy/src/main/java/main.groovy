import groovy.json.JsonOutput
import groovy.json.JsonSlurper
import rx.Observable

class Read {
    int Checkpoint
    int bib
    String gender
    int age
    String time
}

class groupedReads{
    String groupName
    Read read
}

Observable<groupedReads> overallReads(Observable<Read> reads) {
    reads
        .groupBy({Read item -> item.Checkpoint})
        .flatMap({group->
                  group.map({Read item->
                      new groupedReads(read: item, groupName: "Overall Checkpoint ${item.Checkpoint}")
                  })
    })
}
Observable<groupedReads> genderReads(Observable<Read> reads) {
    reads
            .groupBy({Read item -> Tuple.any {item.Checkpoint; item.gender}})
            .flatMap({group->
                        group.map({Read item->
                            new groupedReads(read: item, groupName: "${item.gender} Checkpoint ${item.Checkpoint}")
                        })
    })
}
Observable<groupedReads> genderAgeReads(Observable<Read> reads) {
    reads
            .groupBy({Read item -> Tuple.any {item.Checkpoint; item.gender; (item.age +2) / 5}})
            .flatMap({group->
        group.map({Read item->
            new groupedReads(read: item, groupName: "${item.gender} ${item.age * 5 -2}-${(item.age+1) * 5 -2} Checkpoint ${item.Checkpoint}")
        })
    })
}

Observable<groupedReads> groupAllReads(Observable<Read> reads) {
    overallReads(reads)
    .merge(genderReads(reads))
    .merge(genderAgeReads(reads))
}

Observable<groupedReads> stream(Observable<String> input){
    JsonSlurper json = new JsonSlurper()
    Observable<Read> mapped = input.map({f -> json.parseText(f)})
        .map({ f-> new Read(
            age: f["age"],
            bib: f["bib"],
            Checkpoint: f["checkpoint"],
            time: f["time"],
            gender: f["gender"])})
        .publish()
        .refCount()

    return groupAllReads(mapped);
}

void main() {

    def input = Observable.from(System.in.readLines())
    stream(input)
            .subscribe({f-> println(JsonOutput.toJson({groupName:f.groupName; bib:f.read.bib; time:f.read.time; age:f.read.age}))},
            {err-> println(JsonOutput.toJson(err))},
            { -> println("done...")})
}

main()