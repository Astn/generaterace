import rx.lang.scala.Observable

import scala.util.parsing.json.{JSONObject, JSON}

object StreamingScala {

  class GroupedRead(gn:String,r:Read) {
    var groupName: String = gn
    var read : Read = r
  }

  class Read() {
    var time: String = ""
    var bib: Int = 0
    var gender: String = ""
    var age : Int = 0
    var checkpoint : Int = 0
  }
  def overallReads(input:Observable[Read])=
    input
      .groupBy(x => x.checkpoint )
      .flatMap{ case (i, group) =>
                group.map(item => new GroupedRead(gn=s"Overall Checkpoint ${item.checkpoint}", r = item))}

  def genderReads(input:Observable[Read]):Observable[GroupedRead] =
    input
    .groupBy(x => (x.checkpoint, x.gender) )
    .flatMap{ case (i, group) =>
                group.map(item => new GroupedRead(gn=s"${item.gender} Checkpoint ${item.checkpoint}", r = item))}

  def genderAgeReads(input:Observable[Read]):Observable[GroupedRead] =
    input
      .groupBy(x => (x.checkpoint, x.gender, (x.age+2)/5))
      .flatMap{ case (i, group) =>
              group.map(item => new GroupedRead(gn=s"${item.gender} ${i._3*5-2}-${(i._3+1)*5-2} Checkpoint ${item.checkpoint}", r = item))}

  def groupAllReads(input:Observable[Read]):Observable[GroupedRead] =
      overallReads(input)
      .merge(genderReads(input))
      .merge(genderAgeReads(input))

  def stream(input:Observable[String]): Observable[Read] = {
      val result = input.map(x=> JSON.parseFull(x))
       .map(x => x.get.asInstanceOf[Map[String,Any]])
       .map(x => new Read {
                  time = x("time").asInstanceOf[String]
                  bib = x("bib").asInstanceOf[Int]
                  gender = x("gender").asInstanceOf[String]
                  age = x("age").asInstanceOf[Int]
                  checkpoint = x("checkpoint").asInstanceOf[Int]
                })
        result.publish.refCount
  }

  def main(args: Array[String]): Unit ={

    val source = Observable.from(iterable=io.Source.stdin.getLines().toIterable)

    groupAllReads(stream(source))
      .map(gr=> s"""{\"groupName\":\"${gr.groupName}\", \"bib\":${gr.read.bib}, \"time\":\"${gr.read.time}\", \"age\":${gr.read.age}}""")
      .subscribe(item=> println(item),
                 err => println(err),
                 ()=> println("done"))

    println("hello world")
  }



}