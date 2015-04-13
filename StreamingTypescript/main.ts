/**
 * Created by msdn_000 on 4/13/2015.
 */


var fileinput = require('fileinput');
var fs = require('fs');
var Rx = require('rx-lite');
Rx.Node = require('rx-node');
var lazy = require('lazy');

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

function main(){

    var subscription = getInput()
        .take(1000)
        .subscribe(function (x) { console.log(x); });
}


main();