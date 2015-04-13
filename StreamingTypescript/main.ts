/**
 * Created by msdn_000 on 4/13/2015.
 */


var fileinput = require('fileinput');
var fs = require('fs');
var Rx = require('rx-lite');
Rx.Node = require('rx-node');
var lazy = require('lazy');

/*
process.argv.forEach(function(val, index) {
    console.log(index + " : " + val);
});
*/

function endsWith(str, suffix) {
    return str.indexOf(suffix, str.length - suffix.length) !== -1;
}

function getInput(){
    if(process.argv > 2){
        return Rx.Observable.fromEvent(fileinput.input(),'line')
            .map(function (line) {return line.toString('utf8');})
            .map(function (line) {return line.replace(fixNewLine,"");});
    } else {
        return Rx.Node.fromReadableStream(process.stdin)
            .selectMany(function (line) {return line.toString().split(/\r\n/)})
            .windowWithCount(2,1)
            .selectMany(function (l) {
                return l.reduce(function (acc,x){
                    if(endsWith(acc,"}") === true)
                        return acc;
                    else return acc + x;
                }, "");
            })
            .filter(function (item){
                return item[0] === "{";
            });
    }
}
var fixNewLine = new RegExp("(\r)?\n");

function main(){

    var subscription = getInput()
        .take(100)
        .subscribe(function (x) { console.log(x); });
}


main();