/**
 * Created by msdn_000 on 4/13/2015.
 */
var fileinput = require('fileinput');
var fs = require('fs');
var Rx = require('rx');
Rx.Node = require('rx-node');
;
;
var fixNewLine = new RegExp("(\r)?\n");
function toOutputType(item) {
    return {
        "groupName": item.name,
        "bib": item.item.bib,
        "time": item.item.time,
        "age": item.item.age
    };
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
    return { "checkpoint": item.checkpoint, "gender": item.gender, "age": item.age };
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
        return Rx.Observable.fromEvent(fileinput.input(), 'line').map(function (line) {
            return line.toString('utf8');
        }).map(function (line) {
            return line.replace(fixNewLine, "");
        });
    }
    else {
        return Rx.Node.fromReadableStream(process.stdin).selectMany(function (line) {
            return line.toString().split(/(\r)?\n/);
        }).windowWithCount(2, 1).selectMany(function (l) {
            return l.reduce(function (acc, x) {
                if (endsWith(acc, "}") === true)
                    return acc;
                else
                    return acc + x;
            }, "");
        }).filter(function (item) {
            return item[0] === "{";
        });
    }
}
function groupReads(reads) {
    var overall = reads.groupBy(byCheckpoint).selectMany(function (group) {
        return group.map(function (item) {
            return { "name": "Overall Checkpoint " + group.key, "item": item };
        });
    });
    var gender = reads.groupBy(byCheckpointGender, itemIdentity, compareByCheckpointGender).selectMany(function (group) {
        return group.map(function (item) {
            return { "name": group.key.gender + " Checkpoint " + group.key.checkpoint, "item": item };
        });
    });
    var genderAge = reads.groupBy(byCheckpointGenderAge, itemIdentity, compareByCheckpointGenderAge).selectMany(function (group) {
        return group.map(function (item) {
            return {
                "name": group.key.gender + " " + group.key.age + " Checkpoint " + group.key.checkpoint,
                "item": item
            };
        });
    });
    return overall.merge(gender).merge(genderAge);
}
function toReadType(line) {
    return JSON.parse(line);
}
function stream(lines) {
    var reads = lines.map(toReadType).publish().refCount();
    return groupReads(reads).map(toOutputType);
}
function main() {
    var lines = getInput();
    var subscription = stream(lines).subscribe(function (x) {
        process.stdout.write(JSON.stringify(x) + '\n');
    });
}
main();
//# sourceMappingURL=main.js.map