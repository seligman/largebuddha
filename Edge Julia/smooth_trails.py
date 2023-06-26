#!/usr/bin/env python3

from collections import deque
from datetime import datetime, timedelta
from scottsutils.command_opts import opt, main_entry
from scottsutils.window_title import set_title
from urllib.request import urlopen
import mandelbrot_native_helper
import numpy as np
import lzma, math, multiprocessing, os, pickle, psutil
import socket, sqlite3, statistics, threading, time

TARGET = "edge_00039_e06x177_many"
DATA_FILE = os.path.join("data", TARGET + ".png.dat")
DB_FILE = TARGET + ".smooth.db"
OUTPUT_FILE = TARGET + ".smooth.dat"
FINAL_FRAME_COUNT = 15_000

_save_history = False
@opt("Save all history during do_work")
def opt_history():
    global _save_history
    _save_history = True
    show_msg("Option: Save All History set to True")

_skip_load = False
@opt("Skip loading of frame data")
def opt_skip():
    global _skip_load
    _skip_load = True
    show_msg("Option: Skip Load set to True")

@opt("Limit number of target frames")
def opt_limit(perc):
    global FINAL_FRAME_COUNT
    FINAL_FRAME_COUNT = int(FINAL_FRAME_COUNT * (float(perc) / 100.0))
    show_msg(f"Option: Using {FINAL_FRAME_COUNT:,} frames")

@opt("Show some stats on the database")
def stats():
    db, _ = open_db()
    def run_sql(sql):
        return db.execute(sql).fetchone()[0]
    total = run_sql("SELECT count(*) FROM frames;")
    show_msg(f"Total of {total:,} frames")
    frame_data = run_sql("SELECT count(*) FROM frames WHERE has_frame_data = 1;")
    show_msg(f"Total of {frame_data:,} frames with frame data, or {frame_data/total*100:.2f}%")
    to_use = run_sql("SELECT count(*) FROM frames WHERE has_frame_data = 1 AND use_frame=1;")
    show_msg(f"Total of {frame_data:,} frames set to use, or {to_use/frame_data*100:.2f}%")

_enter_worker_fn = "abort.txt"
def enter_worker():
    try:
        while True:
            try:
                if input() in {"exit", "x"}: break
            except EOFError:
                pass
            except KeyboardInterrupt:
                pass
            if os.path.isfile(_enter_worker_fn):
                show_msg("Removed abort file")
                os.unlink(_enter_worker_fn)
            else:
                show_msg("Created abort file")
                with open(_enter_worker_fn, "wt") as f:
                    f.write("--")
    except:
        return

def start_enter_worker():
    threading.Thread(target=enter_worker, daemon=True).start()

def show_msg(value):
    # Simple helper to show a message with a timestamp
    print(datetime.utcnow().strftime("%d %H:%M:%S: ") + value)

def open_db():
    existing_file = os.path.isfile(DB_FILE)
    db = sqlite3.connect(DB_FILE)
    db.execute("PRAGMA journal_mode=wal;")

    if not existing_file:
        db.execute("""
            CREATE TABLE IF NOT EXISTS
                frames(
                    frame_no INT NOT NULL,
                    xy_data BLOB NOT NULL,
                    has_frame_data INT NOT NULL,
                    use_frame INT NOT NULL,
                    frame_data BLOB
                );
        """)
        db.execute("""
            CREATE TABLE IF NOT EXISTS
                diffs(
                    frame_a INT NOT NULL,
                    frame_b INT NOT NULL,
                    diff INT NOT NULL
                );
        """)
        db.execute("CREATE INDEX IF NOT EXISTS diffs_a_b_index ON diffs(frame_a, frame_b);")
        db.execute("CREATE INDEX IF NOT EXISTS frames_no_index ON frames(frame_no);")
        db.execute("CREATE INDEX IF NOT EXISTS frames_use_index ON frames(use_frame);")
        db.execute("CREATE INDEX IF NOT EXISTS frames_data_index ON frames(has_frame_data);")
        db.execute("CREATE INDEX IF NOT EXISTS frames_use_has_index ON frames(use_frame, has_frame_data);")
        db.execute("CREATE INDEX IF NOT EXISTS frames_use_has_no_index ON frames(use_frame, has_frame_data, frame_no);")
        db.commit()
    return db, existing_file

def load_frames(db):
    show_msg("Loading frame data into DB...")
    with open(DATA_FILE, "rb") as f:
        data = pickle.load(f)
    inserts = [(i, pickle.dumps(row), 0, None, 1) for (i, row) in enumerate(data)]
    if db.execute("SELECT count(*) FROM frames;").fetchone()[0] != len(inserts):
        db.execute("DELETE FROM frames;")
        db.execute("DELETE FROM diffs;")
        db.executemany("INSERT INTO frames(frame_no, xy_data, has_frame_data, frame_data, use_frame) VALUES (?, ?, ?, ?, ?);", inserts)
        db.commit()
        show_msg(f"DB populated with {len(inserts):,} frames")
    else:
        show_msg("DB has frame data already")

def calculate_frame(job, ignore_abort=False):
    if not ignore_abort:
        if os.path.isfile("abort.txt"):
            return None

    frame_no, xy_data = job

    width, height = 2000, 2000
    max_iters = 250
    data = np.zeros((width, height), dtype=np.uint8)

    jx, jy = pickle.loads(xy_data)
    for y in range(height):
        y_point = ((y / height) * 4) - 2
        for x in range(width):
            x_point = ((x / width) * 4) - 2

            in_set, escaped_at, final_dist = mandelbrot_native_helper.calc(x_point, y_point, True, jx, jy, max_iters)
            if in_set == 1:
                escaped_at = max_iters + 1
            data[x, y] = escaped_at

    data = data.flatten()
    data = data.tobytes()
    data = lzma.compress(data)

    return (frame_no, data)

@opt("Run a server to serve up work units", name="server")
def run_server():
    start_enter_worker()
    set_title("Smooth Server")

    if os.path.isfile("abort.txt"):
        os.unlink("abort.txt")

    import flask
    import logging
    log = logging.getLogger('werkzeug')
    log.setLevel(logging.ERROR)
    db, _ = open_db()

    total_count = db.execute("SELECT count(*) FROM frames;").fetchone()[0]
    done_count = db.execute("SELECT count(*) FROM frames WHERE has_frame_data = 1;").fetchone()[0]
    todo = deque(row for row in db.execute("SELECT frame_no, xy_data FROM frames WHERE has_frame_data = 0;"))

    def get_work_item():
        ret = []
        for _ in range(5):
            if len(todo) > 0:
                ret.append(todo.popleft())
            else:
                break
        if len(ret) > 0:
            return pickle.dumps(ret)
        else:
            return b''

    batch = []
    next_msg = datetime.utcnow()
    recently_done = {}

    def get_hello_page():
        return b'HELLO'

    def get_flush_items():
        nonlocal batch, next_msg, recently_done, done_count
        flushed = len(batch)
        if len(batch) > 0:
            db.executemany("UPDATE frames SET frame_data = ?, has_frame_data = 1 WHERE frame_no = ?;", batch)
            db.commit()
            batch = []
        msg = f"Flushed {flushed:,} items"
        show_msg(msg)
        msg += "\n"
        return msg.encode("utf-8")

    def post_work_item():
        nonlocal batch, next_msg, recently_done, done_count
        job = flask.request.get_data()
        job = pickle.loads(job)
        recently_done[job['from']] = recently_done.get(job['from'], 0) + len(job['batch'])
        done_count += len(job['batch'])
        for frame_no, data in job['batch']:
            batch.append((data, frame_no))

        if len(batch) >= 5_000:
            db.executemany("UPDATE frames SET frame_data = ?, has_frame_data = 1 WHERE frame_no = ?;", batch)
            db.commit()
            batch = []

        if datetime.utcnow() >= next_msg:
            temp = []
            for key in sorted(recently_done):
                value = recently_done[key]
                temp.append(f"{key}:{value:3d}")
            temp = " / ".join(temp)
            recently_done = {}
            show_msg(f"{done_count / total_count * 100:.2f}% done, {temp}")
            while datetime.utcnow() >= next_msg:
                next_msg += timedelta(seconds=60)
        return b'OK'

    my_ip = socket.gethostbyname(socket.gethostname())
    port = 5566
    show_msg(f"Running server at {my_ip}, port {port}")

    app = flask.Flask(__name__, static_url_path='')
    app.add_url_rule('/get', 'get-main-page', get_work_item)
    app.add_url_rule('/hello', 'get-hello-page', get_hello_page)
    app.add_url_rule('/flush', 'get-flush-page', get_flush_items)
    app.add_url_rule('/done', 'work-item-done', post_work_item, methods=['POST'])
    app.run(debug=False, threaded=False, port=port, host="0.0.0.0")

def safe_urlopen(url, data=None):
    bail_at = datetime.utcnow() + timedelta(minutes=5)
    to_sleep = 1
    while True:
        try:
            resp = urlopen(url, data=data)
            resp = resp.read()
            return resp
        except:
            if datetime.utcnow() >= bail_at:
                return None
            to_sleep = min(to_sleep + 5, 30)
            time.sleep(to_sleep)

def run_client_internal(queue, server):
    while not os.path.isfile("abort.txt"):
        jobs = safe_urlopen(server + "get")
        if jobs is None or len(jobs) == 0:
            queue.put("No more jobs!")
            break
        jobs = pickle.loads(jobs)
        batch = []
        for job in jobs:
            result = calculate_frame(job, ignore_abort=True)
            if result is not None:
                batch.append(result)
        batch = pickle.dumps({
            "from": socket.gethostname(),
            "batch": batch,
        })
        resp = safe_urlopen(server + "done", batch)
        if resp != b'OK':
            queue.put(f"Got error response from server, giving up")
            break
        queue.put(jobs[0][0])
    queue.put(None)

def fix_server_name(server):
    if "." not in server:
        server = "192.168.1." + server
    if ":" not in server:
        server = server + ":5566"
    if not server.startswith("http://"):
        server = "http://" + server
    if not server.endswith("/"):
        server += "/"
    return server

@opt("Flush server", name="flush")
def flush_server(server="127.0.0.1"):
    server = fix_server_name(server)
    resp = urlopen(server + "flush").read()
    show_msg(resp.decode("utf-8"))

@opt("Run a client to get work units from a server", name="client")
def run_client(server="127.0.0.1"):
    set_title("Smooth Client")
    server = fix_server_name(server)

    start_enter_worker()
    
    show_msg(f"Starting client pointing to {server}")

    if urlopen(server + "hello").read() == b'HELLO':
        show_msg("Got hello response from server")
    else:
        raise Exception("Unable to talk to server")

    if os.path.isfile("abort.txt"):
        os.unlink("abort.txt")
    workers = psutil.cpu_count(logical=False)
    queue = multiprocessing.Queue()

    procs = []
    for _ in range(workers):
        proc = multiprocessing.Process(target=run_client_internal, args=(queue, server))
        proc.start()
        procs.append(proc)

    next_msg = datetime.utcnow()
    batches = []
    while workers > 0:
        msg = queue.get()
        if msg is None:
            workers -= 1
        else:
            batches.append(msg)
            if datetime.utcnow() >= next_msg:
                show_msg(f"Sent off {len(batches):,} {'batch' if len(batches) == 1 else 'batches'}, last at {max(batches):,}...")
                batches = []
                while datetime.utcnow() >= next_msg:
                    next_msg += timedelta(seconds=60)

    for proc in procs:
        proc.join()
    
def populate_frames(db):
    if _skip_load:
        show_msg("Skipping loading frame data!")
        return

    show_msg("Calculating all frames from DB...")
    todo = [row for row in db.execute("SELECT frame_no, xy_data FROM frames WHERE has_frame_data = 0;")]
    total_count = db.execute("SELECT count(*) FROM frames;").fetchone()[0]
    
    workers = psutil.cpu_count(logical=False)
    left = len(todo)
    inserts = []
    next_msg = datetime.utcnow()
    with multiprocessing.Pool(workers) as pool:
        for job in pool.imap_unordered(calculate_frame, todo):
            if job is not None:
                frame_no, data = job
                left -= 1
                if datetime.utcnow() >= next_msg:
                    show_msg(f"Done with {frame_no:,}, {left:,} left, {frame_no / total_count * 100:.2f}%")
                    while datetime.utcnow() >= next_msg:
                        next_msg += timedelta(seconds=15)
                inserts.append(((data, frame_no)))
                if len(inserts) >= 5_000:
                    db.executemany("UPDATE frames SET frame_data = ?, has_frame_data=1 WHERE frame_no = ?;", inserts)
                    db.commit()
                    inserts = []
            else:
                break

    if len(inserts) > 0:
        db.executemany("UPDATE frames SET frame_data = ?, has_frame_data=1 WHERE frame_no = ?;", inserts)
        db.commit()

_db = None
def compare_frames(job, db=None):
    if os.path.isfile("abort.txt"):
        return None

    global _db
    if db is not None:
        _db = db
    else:
        if _db is None:
            _db, _ = open_db()

    def load_frame(val):
        ret = _db.execute("SELECT frame_data FROM frames WHERE frame_no=?;", (val,)).fetchone()[0]
        ret = lzma.decompress(ret)
        ret = np.frombuffer(ret, np.uint8)
        return ret.astype(np.int16)

    if isinstance(job, list):
        last_frame_no = None
        last_frame = None

        ret = []

        for frame_no in job:
            frame = load_frame(frame_no)
            if last_frame is not None:
                frame_diff = int(np.sum(np.abs(np.subtract(last_frame, frame))))
                ret.append((last_frame_no, frame_no, frame_diff))
            last_frame, last_frame_no = frame, frame_no
        
        return ret
    else:
        a, b = job

        data_a = load_frame(a)
        data_b = load_frame(b)

        frame_diff = int(np.sum(np.abs(np.subtract(data_b, data_a))))

        return a, b, frame_diff

def create_batches(temp):
    todo = []
    batch_size = 50
    for offset in range(0, len(temp), batch_size):
        if os.path.isfile("abort.txt"):
            break
        yield temp[offset:offset+(batch_size+1)]

def prepare_initial_diffs(db, test_mode=False):
    show_msg("Finding items to update")
    if db.execute("SELECT count(*) FROM diffs;").fetchone()[0] > 0:
        show_msg("Already has diff data, skipping finding more!")
        return
    temp = [x for x, in db.execute("SELECT frame_no FROM frames WHERE has_frame_data = 1 ORDER BY frame_no;")]
    left = len(temp) - 1
    total = left

    if left > 0:
        show_msg("Find initial diffs for all frames...")
        next_msg = datetime.utcnow()
        workers = psutil.cpu_count(logical=False)
        inserts = []
        with multiprocessing.Pool(workers) as pool:
            for job in pool.imap_unordered(compare_frames, create_batches(temp)):
                if job is not None:
                    for a, b, diff in job:
                        left -= 1 
                        if datetime.utcnow() >= next_msg:
                            perc = (1 - (left / total)) * 100
                            show_msg(f"Diffs: {a} -> {b} = {diff:10d}, {perc:.2f}%, {left:,} left")
                            while datetime.utcnow() >= next_msg:
                                next_msg += timedelta(seconds=60)
                        inserts.append((a, b, diff))
                        if len(inserts) >= 25_000:
                            if not test_mode:
                                db.executemany("INSERT INTO diffs(frame_a, frame_b, diff) VALUES (?, ?, ?);", inserts)
                                db.commit()
                            inserts = []

        if len(inserts) > 0:
            if not test_mode:
                db.executemany("INSERT INTO diffs(frame_a, frame_b, diff) VALUES (?, ?, ?);", inserts)
                db.commit()

@opt("Reset use frames to use all frames")
def reset_frames():
    db, _ = open_db()
    show_msg(f"Starting with {db.execute('SELECT count(*) FROM frames WHERE use_frame=1;').fetchone()[0]:,} frames enabled")
    db.execute("UPDATE frames SET use_frame=1 WHERE use_frame=0;")
    db.commit()
    show_msg(f"Done with {db.execute('SELECT count(*) FROM frames WHERE use_frame=1;').fetchone()[0]:,} frames enabled")

@opt("Reset all diff data")
def reset_diffs():
    db, _ = open_db()
    show_msg(f"Starting with {db.execute('SELECT count(*) FROM diffs;').fetchone()[0]:,} diff data items")
    db.execute("DELETE FROM diffs;")
    db.commit()
    show_msg(f"Done with {db.execute('SELECT count(*) FROM diffs;').fetchone()[0]:,} diff data items")

class Cache:
    __slots__ = ["diffs", "dists", "pending"]
    def __init__(self, db):
        self.dists = {}
        self.diffs = {}
        self.pending = []
        show_msg("Loading cache data")
        for a, b, diff in db.execute("SELECT frame_a, frame_b, diff FROM diffs;"):
            self.diffs[(a, b)] = diff
        show_msg("Done loading cache")

    def get_dist(self, db, a, b):
        key = (a.frame_no, b.frame_no)
        dist = self.dists.get(key, None)
        if dist is None:
            dist = math.sqrt(((a.x - b.x) ** 2) + ((a.y - b.y) ** 2))
            self.dists[key] = dist
        return dist
    
    def get_diff(self, db, a, b):
        key = (a.frame_no, b.frame_no)
        diff = self.diffs.get(key, None)
        if diff is None:
            diff = compare_frames((a.frame_no, b.frame_no), db=db)[2]
            self.diffs[key] = diff
            self.pending.append((a.frame_no, b.frame_no, diff))
        return diff

    def flush_diff(self, db):
        if len(self.pending) > 0:
            db.executemany("INSERT INTO diffs(frame_a, frame_b, diff) VALUES (?, ?, ?);", self.pending)
            db.commit()
            self.pending = []

class FrameInfo:
    __slots__ = ['frame_no', 'x', 'y', 'dist', 'diff']
    def __init__(self, frame_no, x, y):
        self.frame_no = frame_no
        self.x = x
        self.y = y
        self.dist = 0
        self.diff = 0

    def __lt__(self, other):
        return self.diff < other.diff

def smooth_frames(db, test_mode=False):
    show_msg("Prepare data for finding missing frames")

    limit = ""
    if test_mode:
        limit = " LIMIT 500000 "

    keep = set()
    all_frames = []
    
    range_x, range_y = Range(), Range()
    for frame_no, xy_data in db.execute("SELECT frame_no, xy_data FROM frames WHERE use_frame=1 AND has_frame_data=1 ORDER BY frame_no" + limit + ";"):
        x, y = pickle.loads(xy_data)
        range_x.track(x, frame_no)
        range_y.track(y, frame_no)
        all_frames.append(FrameInfo(frame_no, x, y))
    keep.add(all_frames[0].frame_no)
    keep.add(all_frames[-1].frame_no)
    keep.add(range_x.min_data)
    keep.add(range_x.max_data)
    keep.add(range_y.min_data)
    keep.add(range_y.max_data)

    if _save_history:
        with open("history.csv", "wt") as f:
            f.write(",diffs,,,,distance,,,\n")
            f.write(",min,max,avg,stddev,min,max,avg,stddev\n")

    starting_frames = len(all_frames)
    removed = []
    cache = Cache(db)
    show_msg("Cache frame information")
    for i, frame in enumerate(all_frames):
        if frame.frame_no not in keep:
            frame.dist = cache.get_dist(db, all_frames[i-1], all_frames[i+1])
            frame.diff = cache.get_diff(db, frame, all_frames[i+1])
    cache.flush_diff(db)

    next_msg = datetime.utcnow()
    slow_down = next_msg + timedelta(minutes=5)
    updates = []
    while len(all_frames) > FINAL_FRAME_COUNT:
        # hq = [x for x in all_frames if x.dist <= 0.01 and x.frame_no not in keep]
        # heapq.heapify(hq)
        # best_frame = heapq.heappop(hq)
        # best = all_frames.index(best_frame)
        # best_diff = best_frame.diff

        # best_frame, best = min(((x, i) for i, x in enumerate(all_frames) if x.dist <= 0.01 and x.frame_no not in keep), key=lambda x: x[0].diff)
        # best_diff = best_frame.diff

        best, best_diff = None, 1 << 64
        for i, frame in enumerate(all_frames):
            if frame.dist <= 0.01 and frame.diff < best_diff and frame.frame_no not in keep:
                best_diff, best = frame.diff, i

        if _save_history:
            with open("history.csv", "at") as f:
                def calc_vals(vals):
                    return min(vals), max(vals), sum(vals) / len(vals), statistics.stdev(vals)
                min_diff, max_diff, avg_diff, stddev_diff = calc_vals([x.diff for x in all_frames])
                min_dist, max_dist, avg_dist, stddev_dist = calc_vals([x.dist for x in all_frames])
                f.write(f"{len(all_frames)},{min_diff},{max_diff},{avg_diff},{stddev_diff},{min_dist},{max_dist},{avg_dist},{stddev_dist}\n")

        if not test_mode:
            updates.append((all_frames[best].frame_no,))
            if len(updates) >= 10_000:
                db.executemany("UPDATE frames SET use_frame=0 WHERE frame_no=?;", updates)
                cache.flush_diff(db)
                db.commit()
                updates = []
        all_frames.pop(best)
        removed.append(best_diff)
        for i in range(max(0, best-1), min(len(all_frames)-2,best+1)+1):
            all_frames[i].diff = cache.get_diff(db, all_frames[i], all_frames[i+1])
        for i in range(max(1, best-1), min(len(all_frames)-2,best+1)+1):
            all_frames[i].dist = cache.get_dist(db, all_frames[i-1], all_frames[i+1])

        if datetime.utcnow() >= next_msg:
            avg = sum(removed) / len(removed)
            perc = (1 - (len(all_frames) - FINAL_FRAME_COUNT) / (starting_frames - FINAL_FRAME_COUNT)) * 100
            show_msg(f"Removed {len(removed):4d}, {min(removed):7d} -> {int(avg):7d} -> {max(removed):7d}, {len(all_frames):,} left, {perc:.2f}% done")
            removed = []
            while datetime.utcnow() >= next_msg:
                if datetime.utcnow() >= slow_down:
                    next_msg += timedelta(seconds=60)
                else:
                    next_msg += timedelta(seconds=15)
            if os.path.isfile("abort.txt"):
                break

    if len(updates) > 0 or len(cache.pending) > 0:
        if len(updates) > 0:
            db.executemany("UPDATE frames SET use_frame=0 WHERE frame_no=?;", updates)
        cache.flush_diff(db)
        db.commit()


class Range:
    __slots__ = ["min_value", "max_value", "min_data", "max_data"]
    def __init__(self):
        self.min_value = None
        self.max_value = None
        self.min_data = None
        self.max_data = None
    
    def track(self, value, data=None):
        if self.min_value is None or value < self.min_value:
            self.min_value = value
            self.min_data = data
        if self.max_value is None or value > self.max_value:
            self.max_value = value
            self.max_data = data

@opt("Test entry point for different test things")
def test():
    if os.path.isfile("abort.txt"):
        os.unlink("abort.txt")
    start_enter_worker()

    db, _ = open_db()
    show_msg("Running mini-test mode of prepare_initial_diffs")
    started = datetime.utcnow()
    prepare_initial_diffs(db, True)
    ended = datetime.utcnow()
    show_msg(f"That took {int((ended - started).total_seconds()):,} seconds")

def write_final(db):
    with open(OUTPUT_FILE, "wb") as f:
        data = []
        for xy_data, in db.execute("SELECT xy_data FROM frames WHERE use_frame=1 AND has_frame_data = 1 ORDER BY frame_no;"):
            data.append(pickle.loads(xy_data))
        pickle.dump(data, f)
        show_msg(f"Created data file of {len(data):,} frames")

@opt("Perform all work to find smooth trail")
def do_work():
    if os.path.isfile("abort.txt"):
        os.unlink("abort.txt")

    start_enter_worker()

    db, existing_file = open_db()
    steps = [
        load_frames,
        populate_frames,
        prepare_initial_diffs,
        smooth_frames,
        write_final,
    ]
    for step in steps:
        if os.path.isfile("abort.txt"):
            show_msg("Abort file detected")
            break
        step(db)

if __name__ == "__main__":
    main_entry('func')

