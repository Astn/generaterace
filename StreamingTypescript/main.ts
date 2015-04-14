/**
 * Created by msdn_000 on 4/13/2015.
 */



var fileinput = require('fileinput');
var fs = require('fs');
var Rx = require('rx');
Rx.Node = require('rx-node');

function endsWith(str, suffix) {
    return str.indexOf(suffix, str.length - suffix.length) !== -1;
}

var fixNewLine = new RegExp("(\r)?\n");

function getInput(){
    if(process.argv.length > 2){
        return Rx.Observable.fromEvent(fileinput.input(),'line')
            .map(function (line) {return line.toString('utf8');})
            .map(function (line) {return line.replace(fixNewLine,"");});
    } else {
        return Rx.Node.fromReadableStream(process.stdin)
            .selectMany(function (line) {
                return line.toString().split(/\r\n/)
            })
            .windowWithCount(2, 1)
            .selectMany(function (l) {
                return l.reduce(function (acc, x) {
                    if (endsWith(acc, "}") === true)
                        return acc;
                    else return acc + x;
                }, "");
            })
            .filter(function (item) {
                return item[0] === "{";
            });
    }
}

function groupReads(reads: Rx.Observable<{
    bib: number;
    checkpoint: number;
    gender: string;
    age: number;
    time: string }>): Observable<{
    groupName: string;
    bib: number;
    checkpoint: number;
    gender: string;
    age: number;
    time: string }>{

    var overall = reads.groupBy(function (item) {return item.checkpoint;})
                        .selectMany(function (group){
                            return group.map(function(item){
                                return {"name":"Overall Checkpoint " + group.key, "item":item};
                            });
                        });
    var gender = reads.groupBy(function (item) {return {"checkpoint":item.checkpoint,"gender":item.gender};},
                                function (e) {return e;},
                                function (groupA,groupB) {return groupA.checkpoint === groupB.checkpoint && groupA.gender === groupB.gender;})
                        .selectMany(function (group){
                            return group.map(function(item){
                                return {"name": group.key.gender + " Checkpoint " + group.key.checkpoint, "item":item};
                            });
                        });

    var genderAge = reads.groupBy(function (item) {return {"checkpoint":item.checkpoint,"gender":item.gender, "age":item.age};},
                                    function (e) {return e;},
                                    function (groupA,groupB) {return groupA.checkpoint === groupB.checkpoint && groupA.gender === groupB.gender && groupA.age === groupB.age;})
                            .selectMany(function (group){
                                return group.map(function(item){
                                    return {"name": group.key.gender + " " + group.key.age + " Checkpoint " + group.key.checkpoint, "item":item};
                                });
                            });

    return overall.merge(gender).merge(genderAge);
}

function stream(lines: Rx.Observable<string>): Rx.Observable<{
    name:string;
    item:{
        bib: number;
        checkpoint: number;
        gender: string;
        age: number;
        time: string }}>{
    var reads = lines
                .select(function(x){return JSON.parse(x);})
                .publish()
                .refCount();

    return groupReads(reads)
        .map(function (item){return {
            "groupName":item.name,
            "bib":item.item.bib,
            "time": item.item.time,
            "age": item.item.age};
        });
}

function main(){

    var lines = getInput();

    var subscription = stream(lines)
        .take(1000)
        .subscribe(function (x) { process.stdout.write(JSON.stringify(x)+'\n'); });
}


main();