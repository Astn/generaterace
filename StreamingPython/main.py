import os
import rx
import io
import fileinput
import json

from rx import Observable, Observer, linq


def age_range(group):
    chk, gender, age_group = group
    return "{0}-{1}".format(age_group * 5 - 2, (age_group + 1) * 5 - 2)


def group_reads(reads):
    share = reads.publish().ref_count()

    overall = share \
        .group_by(lambda item: item['checkpoint']) \
        .select_many(lambda group:
                     group.map(lambda item: ("Overall Checkpoint {0}".format(str(group.key)), item)))

    gender = share \
        .group_by(lambda item: (item['checkpoint'], item['gender'])) \
        .select_many(lambda group:
                     group.map(lambda item: ("{0} Checkpoint {1}".format(group.key[1], group.key[0]), item)))

    gender_age = share \
        .group_by(lambda item: (item['checkpoint'], item['gender'], (item['age'] + 2) / 5)) \
        .select_many(lambda group:
                     group.map(lambda item:
                               ("{0} {1} Checkpoint {2}".format(group.key[1], age_range(group.key), group.key[0]),
                                item)))

    return overall \
        .merge(gender) \
        .merge(gender_age)


def to_print_format(groupItem):
    group, item = groupItem
    return {'groupName': group, 'bib': item['bib'], 'time': item['time'], 'age': item['age']}


def main():
    reads = Observable.from_iterable(fileinput.input()) \
        .map(lambda line: io.StringIO(line)) \
        .map(lambda line: json.load(line))

    group_reads(reads) \
        .map(to_print_format) \
        .subscribe(
        lambda x: print(json.dumps(x)),
        lambda ex: print("Error ", ex.Message))


if __name__ == '__main__':
    main()