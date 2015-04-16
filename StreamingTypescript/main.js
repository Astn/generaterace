/// <reference path="typings/tsd.d.ts" />
/**
* Created by msdn_000 on 4/13/2015.
*/
var fileinput = require('fileinput');
var fs = require('fs');
var Rx1 = require('rx');
Rx1.Node = require('rx-node');

var fixNewLine = new RegExp("(\r)?\n");

var SimpleReadImpl = (function () {
    function SimpleReadImpl(bib, checkpoint, gender, age, time) {
        this.bib = bib;
        this.checkpoint = checkpoint;
        this.gender = gender;
        this.age = age;
        this.time = time;
    }
    return SimpleReadImpl;
})();

var GroupedReadImpl = (function () {
    function GroupedReadImpl(name, item) {
        this.name = name;
        this.item = item;
    }
    return GroupedReadImpl;
})();

var GroupedOutputImpl = (function () {
    function GroupedOutputImpl(groupName, bib, time, age) {
        this.groupName = groupName;
        this.bib = bib;
        this.time = time;
        this.age = age;
    }
    return GroupedOutputImpl;
})();

function toOutputType(item) {
    return new GroupedOutputImpl(item.name, item.item.bib, item.item.time, item.item.age);
}

function ageRange(item) {
    return (item.age * 5 - 2) + "-" + ((item.age + 1) * 5 - 2);
}

function itemIdentity(e) {
    return e;
}
function byCheckpoint(item) {
    return item.checkpoint;
}
function byCheckpointGender(item) {
    return { "checkpoint": item.checkpoint, "gender": item.gender };
}
function byCheckpointGenderAge(item) {
    return { "checkpoint": item.checkpoint, "gender": item.gender, "age": Math.floor((item.age + 2) / 5) };
}

function compareByCheckpointGender(groupA, groupB) {
    return groupA.checkpoint === groupB.checkpoint && groupA.gender === groupB.gender;
}
function compareByCheckpointGenderAge(groupA, groupB) {
    return groupA.checkpoint === groupB.checkpoint && groupA.gender === groupB.gender && groupA.age === groupB.age;
}
function endsWith(str, suffix) {
    return str.indexOf(suffix, str.length - suffix.length) !== -1;
}

function getInput() {
    if (process.argv.length > 2) {
        var input = fileinput.input();
        var observable = Rx1.Observable.fromEvent(input, 'line');
        var map = observable.map(function (line) {
            return line.toString().replace(fixNewLine, "");
        });
        return map;
    } else {
        return Rx1.Node.fromReadableStream(process.stdin).selectMany(function (line) {
            return line.toString().split(fixNewLine);
        }).windowWithCount(2, 1).selectMany(function (l) {
            return l.reduce(function (acc, x) {
                if (endsWith(acc, "}"))
                    return acc;
                else
                    return acc + x;
            }, "");
        }).filter(function (item) {
            return (item[0] === "{");
        });
    }
}

function groupReads(reads) {
    var overall = reads.groupBy(byCheckpoint).selectMany(function (group) {
        return group.map(function (item) {
            var impl = new GroupedReadImpl("Overall Checkpoint " + group.key, item);
            return impl;
        });
    });

    var gender = reads.groupBy(byCheckpointGender, itemIdentity, compareByCheckpointGender).selectMany(function (group) {
        return group.map(function (item) {
            var impl = new GroupedReadImpl(group.key.gender + " Checkpoint " + group.key.checkpoint, item);
            return impl;
        });
    });

    var genderAge = reads.groupBy(byCheckpointGenderAge, itemIdentity, compareByCheckpointGenderAge).selectMany(function (group) {
        return group.map(function (item) {
            var impl = new GroupedReadImpl(group.key.gender + " " + ageRange(group.key) + " Checkpoint " + group.key.checkpoint, item);
            return impl;
        });
    });

    return overall.merge(gender).merge(genderAge);
}

function toReadType(line) {
    var data = JSON.parse(line);

    return new SimpleReadImpl(data.bib, data.checkpoint, data.gender, data.age, data.time);
}

function stream(lines) {
    var map = lines.map(toReadType);

    var reads = map.distinctUntilChanged().publish().refCount();

    return groupReads(reads).map(toOutputType);
}

function main() {
    var lines = getInput();

    var subscription = stream(lines).subscribe(function (x) {
        process.stdout.write(JSON.stringify(x) + '\r\n');
    });
}

main();
//# sourceMappingURL=main.js.map
