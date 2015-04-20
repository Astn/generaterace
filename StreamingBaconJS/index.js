/**
 * Created by msdn_000 on 4/13/2015.
 */

var fileinput = require('fileinput');
var Bacon = require('baconjs');

function getIdFromObject(key) {
  var id = "";
  if(typeof key == 'object') {
    for(var i in key) {
      id += key[i].toString();
    }
  } else {
    id = key.toString();
  }

  return id;
}

Bacon.Observable.prototype.groupBy = function(keyF, limitF, comparerF) {
  if(typeof limitF === 'undefined') {
    limitF = itemIdentity;
  }

  if(typeof comparerF == 'undefined') {
    comparerF = function() {
      return true;
    };
  }

  var streams = {},
      src = this;

  return src.filter(function(x) {
    var key = keyF(x),
        id = getIdFromObject(key);
    return !streams[id];
  }).map(function (x) {
    var key = keyF(x),
        id = getIdFromObject(key);

    similar = src.filter(function(y) {
      var idY = getIdFromObject(keyF(y));
      return idY == id && comparerF(x, y);
    });

    data = Bacon.once(x).concat(similar);

    limited = limitF(data).withHandler(function(event) {
      if(event.isEnd()) {
        return delete streams[id];
      }

      return this.push(event);
    });

    streams[id] = limited;
    return streams[id];
  });
};

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
  return compareByCheckpointGender(groupA, groupB) && groupA.age === groupB.age;
}
function endsWith(str, suffix) {
  return str.indexOf(suffix, str.length - suffix.length) !== -1;
}
function getInput() {
  if (process.argv.length > 2) {
    return Bacon.fromEvent(fileinput.input(), 'line').map(function (line) {
      return line.toString('utf8');
    }).map(function (line) {
      return line.replace(fixNewLine, "");
    });
  } else {
    return Bacon.fromBinder(function(sink) {
      process.stdin.on('data', function(buff) {
        sink(buff.toString());
      });
    }).flatMap(function (line) {
      return Bacon.fromArray(line.toString().split(/(\r)?\n/));
    }).slidingWindow(2, 1).flatMap(function (l) {
      return l.reduce(function (acc, x) {
        if (endsWith(acc, "}") === true) {
          return acc;
        } else {
          return acc + x;
        }
      }, "");
    }).filter(function (item) {
      return item[0] === "{";
    });
  }
}
function groupReads(reads) {
  var overall = reads.groupBy(byCheckpoint).flatMap(function (group) {
    return group.map(function (item) {

      return { "name": "Overall Checkpoint " + item.checkpoint, "item": item };
    });
  });

  var gender = reads.groupBy(byCheckpointGender, itemIdentity, compareByCheckpointGender).flatMap(function (group) {
    return group.map(function (item) {
      return { "name": item.gender + " Checkpoint " + item.checkpoint, "item": item };
    });
  });

  var genderAge = reads.groupBy(byCheckpointGenderAge, itemIdentity, compareByCheckpointGenderAge).flatMap(function (group) {
    return group.map(function (item) {
      return {
        "name": item.gender + " " + item.age + " Checkpoint " + item.checkpoint,
        "item": item
      };
    });
  });

  return overall.merge(gender).merge(genderAge);
}
function stream(lines) {
  var reads = lines.map(JSON.parse);
  return groupReads(reads).map(toOutputType);
}
function main() {
  var lines = getInput();
  var subscription = stream(lines).subscribe(function (x) {
    console.log(x);
  });
}
main();
