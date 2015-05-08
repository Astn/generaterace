/// <reference path="typings/tsd.d.ts" />

/**
 * Created by msdn_000 on 4/13/2015.
 */


var fileinput = require('fileinput');
var fs = require('fs');
var rx = require('rx');
rx.Node = require('rx-node');

var fixNewLine = new RegExp("(\r)?\n");

interface SimpleRead {
    bib: number;
    checkpoint: number;
    gender: string;
    age: number;
    time: string;
}

class SimpleReadImpl implements SimpleRead {
    constructor(public bib: number, public checkpoint: number, public gender: string, public age: number, public time: string) {

    }
}

interface IAged {
    age: number;
}

interface GroupedRead {
    name: string;
    item: SimpleRead;
}
class GroupedReadImpl implements GroupedRead {
    constructor(public name: string, public item: SimpleRead) {

    }
}

interface GroupedOutput {
    groupName: string;
    bib: number;
    time: string;
    age: number;
}
class GroupedOutputImpl implements GroupedOutput {
    constructor(public groupName: string, public bib: number, public time: string, public age: number) {

    }
}

function toOutputType(item: GroupedRead): GroupedOutput {
    return new GroupedOutputImpl(item.name, item.item.bib, item.item.time, item.item.age);
}

function ageRange(item: IAged) {
    
    return (item.age * 5 - 2) + "-" + ((item.age + 1) * 5 - 2);
}

function itemIdentity(e: SimpleRead) {
    return e;
}
function byCheckpoint(item: SimpleRead) {
    return item.checkpoint;
}
function byCheckpointGender(item: SimpleRead) {
    return { "checkpoint": item.checkpoint, "gender": item.gender };
}
function byCheckpointGenderAge(item: SimpleRead) {
    return { "checkpoint": item.checkpoint, "gender": item.gender, "age": Math.floor((item.age + 2) / 5) };
}

function compareByCheckpointGender(groupA: any, groupB: any) {
    return groupA.checkpoint === groupB.checkpoint && groupA.gender === groupB.gender;
}
function compareByCheckpointGenderAge(groupA: any, groupB: any) {
    return groupA.checkpoint === groupB.checkpoint && groupA.gender === groupB.gender && groupA.age === groupB.age;
}
function endsWith(str: any, suffix: any) {
    return str.indexOf(suffix, str.length - suffix.length) !== -1;
}

function getInput(): Rx.Observable<string> {

    if (process.argv.length > 2) {
        var input: Node = fileinput.input();
        var observable = rx.Observable.fromEvent(input, 'line');
        var map = observable.map(line => line.toString().replace(fixNewLine, ""));
        return map;
    } else {
        return rx.Node.fromReadableStream(process.stdin)
            .selectMany(line => line.toString().split(fixNewLine))
            .windowWithCount(2, 1)
            .selectMany(l => l.reduce((acc, x) => {
                if (endsWith(acc, "}"))
                    return acc;
                else return acc + x;
            }, ""))
            .filter(item => (item[0] === "{"));
    }
}

function groupReads(reads: Rx.Observable<SimpleRead>): Rx.Observable<GroupedRead> {

    var overall = reads.groupBy(byCheckpoint)
        .selectMany(group => group.map(item => {
            var impl: GroupedRead = new GroupedReadImpl("Overall Checkpoint " + group.key, item);
            return impl;
        }));

    var gender = reads.groupBy(byCheckpointGender, itemIdentity, compareByCheckpointGender)
        .selectMany(group => group.map(item => {
            var impl: GroupedRead = new GroupedReadImpl(group.key.gender + " Checkpoint " + group.key.checkpoint, item);
            return impl;
        }));

    var genderAge = reads.groupBy(byCheckpointGenderAge, itemIdentity, compareByCheckpointGenderAge)
        .selectMany(group => group.map(item => {
            var impl: GroupedRead = new GroupedReadImpl(group.key.gender + " " + ageRange(group.key) + " Checkpoint " + group.key.checkpoint, item);
            return impl;
        }));

    return overall.merge(gender).merge(genderAge);
}

function toReadType(line: string): SimpleRead {

    var data = JSON.parse(line);

    return new SimpleReadImpl(data.bib, data.checkpoint, data.gender, data.age, data.time);
}

function stream(lines: Rx.Observable<string>): Rx.Observable<GroupedOutput> {

    var map: any = lines.map(toReadType);

    var reads: Rx.Observable<SimpleRead> = map
        .distinctUntilChanged()
        .publish()
        .refCount();

    return groupReads(reads)
        .map(toOutputType);
}

function main() {

    var lines = getInput();

    var subscription = stream(lines)
        .subscribe(x => { process.stdout.write(JSON.stringify(x) + '\r\n'); });
}

main();