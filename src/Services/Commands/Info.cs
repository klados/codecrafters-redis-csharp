using codecrafters_redis.Helpers;

namespace codecrafters_redis.Commands;

public class Info
{
    private readonly IDictionary<string, string> _infoData = new Dictionary<string, string>()
    {
        {"SERVER", $"redis_version:6.2.14\r\nredis_git_sha1:00000000\r\nredis_git_dirty:0\r\nredis_build_id:8151bdad9faff58a\r\nredis_mode:standalone\r\nos:Linux 6.10.14-linuxkit aarch64\r\narch_bits:64\r\nmonotonic_clock:POSIX clock_gettime\r\nmultiplexing_api:epoll\r\natomicvar_api:c11-builtin\r\ngcc_version:13.2.1\r\nprocess_id:1\r\nprocess_supervised:no\r\nrun_id:de73f75fe94bc154dd7def7b30815f9d9a13a164\r\ntcp_port:{Config.Port}\r\nserver_time_usec:1743868542084166\r\nhz:10\r\nconfigured_hz:10\r\nexecutable:/data/redis-server\r\nio_threads_active:0\r\n"},
        {"CLIENTS", $"connected_clients:1\r\ncluster_connections:{0}\r\nmaxclients:10000\r\nclient_recent_max_input_buffer:0\r\nclient_recent_max_output_buffer:0\r\nblocked_clients:0\r\ntracking_clients:0\r\nclients_in_timeout_table:0\r\n"},
        {"MEMORY", "used_memory:908456\r\nused_memory_human:887.16K\r\nused_memory_rss:14872576\r\nused_memory_rss_human:14.18M\r\nused_memory_peak:966576\r\nused_memory_peak_human:943.92K\r\nused_memory_peak_perc:93.99%\r\nused_memory_overhead:844704\r\nused_memory_startup:844632\r\nused_memory_dataset:63752\r\nused_memory_dataset_perc:99.89%\r\n"},
        {"PERSISTENCE", "loading:0\r\ncurrent_cow_size:0\r\ncurrent_cow_size_age:0\r\ncurrent_fork_perc:0.00\r\ncurrent_save_keys_processed:0\r\ncurrent_save_keys_total:0\r\nrdb_changes_since_last_save:0\r\nrdb_bgsave_in_progress:0\r\nrdb_last_save_time:1743805034\r\n"},
        {"STATS", ""},
        {"CPU", "used_cpu_sys:171.255456\r\nused_cpu_user:154.064762\r\nused_cpu_sys_children:0.010513\r\nused_cpu_user_children:0.002000\r\nused_cpu_sys_main_thread:171.252137\r\nused_cpu_user_main_thread:154.055873\r\n"},
        {"REPLICATION", $"role:{(Config.IsReplicaOf ? "slave" : "master")}\r\nconnected_slaves:0\r\nmaster_failover_state:no-failover\r\nmaster_replid:091465c549348f7cf6f0c7792e33e7e1fbb5ae74\r\nmaster_replid2:0000000000000000000000000000000000000000\r\nmaster_repl_offset:0\r\nsecond_repl_offset:-1\r\nrepl_backlog_active:0\r\nrepl_backlog_size:1048576\r\nrepl_backlog_first_byte_offset:0\r\nrepl_backlog_histlen:0\r\n"}
    };

    public string InfoCommand(params string[] arguments)
    {
        if (arguments.Length == 0)
        {
            return BuildResponse.Generate('$', string.Join(Environment.NewLine, _infoData.Select(kv=> $"# {kv.Key}\r\n{kv.Value}")));
        }

        return _infoData.TryGetValue(arguments.FirstOrDefault()?.ToUpper(), out var data) ? BuildResponse.Generate('$', data) : "$-1\r\n"; //nil 
    }
}