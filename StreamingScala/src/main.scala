import rx.lang.scala.Observable

import scala.util.parsing.json.{JSONObject, JSON}

object StreamingScala {

  case class GroupedRead(groupName: String, read: Read)

  case class Read(time: String = "", bib: Int = 0, gender: String = "", age: Int = 0, checkpoint: Int = 0)

  def overallReads(input: Observable[Read]) =
    input
        .groupBy(x => x.checkpoint)
        .flatMap { case (i, group) =>
          group.map(item => GroupedRead(groupName = s"Overall Checkpoint ${item.checkpoint}", read = item))
    }

  def genderReads(input: Observable[Read]): Observable[GroupedRead] =
    input
        .groupBy(x => (x.checkpoint, x.gender))
        .flatMap { case (i, group) =>
          group.map(item => GroupedRead(groupName = s"${item.gender} Checkpoint ${item.checkpoint}", read = item))
    }

  def genderAgeReads(input: Observable[Read]): Observable[GroupedRead] =
    input
        .groupBy(x => (x.checkpoint, x.gender, (x.age + 2) / 5))
        .flatMap { case (i, group) =>
          group.map(item => GroupedRead(groupName = s"${item.gender} ${i._3 * 5 - 2}-${(i._3 + 1) * 5 - 2} Checkpoint ${item.checkpoint}", read = item))
    }

  def groupAllReads(input: Observable[Read]): Observable[GroupedRead] =
    overallReads(input)
        .merge(genderReads(input))
        .merge(genderAgeReads(input))

  def stream(input: Observable[String]): Observable[Read] = {
    val result = input.map(JSON.parseFull)
        .map(x => x.get.asInstanceOf[Map[String, Any]])
        .map(x => Read(time = x("time").asInstanceOf[String], bib = x("bib").asInstanceOf[Int], gender = x("gender").asInstanceOf[String], age = x("age").asInstanceOf[Int], checkpoint = x("checkpoint").asInstanceOf[Int]))
    result.publish.refCount
  }

  def main(args: Array[String]): Unit = {
    val source = Observable.from(iterable = io.Source.stdin.getLines().toIterable)
    groupAllReads(stream(source))
        .map(gr => new JSONObject(Map("groupName" -> gr.groupName, "bib" -> gr.read.bib, "time" -> gr.read.time, "age" -> gr.read.age)).toString())
        .subscribe(println, println, () => println("done"))

    println("hello world")
  }


}