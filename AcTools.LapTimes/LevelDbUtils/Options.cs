// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Comparator.cs" company="Microsoft">
//   Copyright (c) 2022 Microsoft Corporation
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
//   with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. Unless
//   required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.See the License for
//   the specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace AcTools.LapTimes.LevelDbUtils {
    /// <summary>
    /// Options to control the behavior of a database (passed to Open)
    /// 
    /// the setter methods for InfoLogger, Env, and Cache only "safe to clean up guarantee". Do not
    /// use Option object if throws.
    /// </summary>
    internal class Options : LevelDbHandle {
        public Options() {
            Handle = LevelDbInterop.leveldb_options_create();
        }

        /// <summary>
        /// If true, the database will be created if it is missing.
        /// </summary>
        public bool CreateIfMissing {
            set { LevelDbInterop.leveldb_options_set_create_if_missing(Handle, value ? (byte)1 : (byte)0); }
        }

        /// <summary>
        /// If true, an error is raised if the database already exists.
        /// </summary>
        public bool ErrorIfExists {
            set { LevelDbInterop.leveldb_options_set_error_if_exists(Handle, value ? (byte)1 : (byte)0); }
        }

        /// <summary>
        /// If true, the implementation will do aggressive checking of the
        /// data it is processing and will stop early if it detects any
        /// errors.  This may have unforeseen ramifications: for example, a
        /// corruption of one DB entry may cause a large number of entries to
        /// become unreadable or for the entire DB to become unopenable.
        /// </summary>
        public bool ParanoidChecks {
            set { LevelDbInterop.leveldb_options_set_paranoid_checks(Handle, value ? (byte)1 : (byte)0); }
        }

        /// <summary>
        /// Use the specified Env object to interact with the environment,
        /// e.g. to read/write files, schedule background work, etc.
        /// </summary>
        public Env Env {
            set {
                LevelDbInterop.leveldb_options_set_env(Handle, value.Handle);
                InnerEnv = value;
            }
            get { return InnerEnv; }
        }

        // Any internal progress/error information generated by the db will
        // be written to info_log if it is non-NULL, or to a file stored
        // in the same directory as the DB contents if info_log is NULL.

        /// <summary>
        /// Amount of data to build up in memory (backed by an unsorted log
        /// on disk) before converting to a sorted on-disk file.
        ///
        /// Larger values increase performance, especially during bulk loads.
        /// Up to two write buffers may be held in memory at the same time,
        /// so you may wish to adjust this parameter to control memory usage.
        /// Also, a larger write buffer will result in a longer recovery time
        /// the next time the database is opened.
        ///
        /// Default: 4MB
        /// </summary>
        public long WriteBufferSize {
            set { LevelDbInterop.leveldb_options_set_write_buffer_size(Handle, value); }
        }

        /// <summary>
        /// Number of open files that can be used by the DB.  You may need to
        /// increase this if your database has a large working set (budget
        /// one open file per 2MB of working set).
        ///
        /// Default: 1000
        /// </summary>
        public int MaxOpenFiles {
            set { LevelDbInterop.leveldb_options_set_max_open_files(Handle, value); }
        }

        /// <summary>
        /// Control over blocks (user data is stored in a set of blocks, and
        /// a block is the unit of reading from disk).
        ///
        /// If not set, leveldb will automatically create and use an 8MB internal cache.
        /// </summary>
        public Cache Cache {
            set {
                LevelDbInterop.leveldb_options_set_cache(Handle, value.Handle);
                InnerCache = value;
            }
            get { return InnerCache; }
        }

        public Comparator Comparator {
            set {
                LevelDbInterop.leveldb_options_set_comparator(Handle, value.Handle);
                InnerComparator = value;
            }
            get { return InnerComparator; }
        }

        /// <summary>
        /// Approximate size of user data packed per block.  Note that the
        /// block size specified here corresponds to uncompressed data.  The
        /// actual size of the unit read from disk may be smaller if
        /// compression is enabled.  This parameter can be changed dynamically.
        ///
        /// Default: 4K
        /// </summary>
        public long BlockSize {
            set { LevelDbInterop.leveldb_options_set_block_size(Handle, value); }
        }

        /// <summary>
        /// Number of keys between restart points for delta encoding of keys.
        /// This parameter can be changed dynamically.  
        /// Most clients should leave this parameter alone.
        ///
        /// Default: 16
        /// </summary>
        public int RestartInterval {
            set { LevelDbInterop.leveldb_options_set_block_restart_interval(Handle, value); }
        }

        /// <summary>
        /// Compress blocks using the specified compression algorithm.  
        /// This parameter can be changed dynamically.
        ///
        /// Default: kSnappyCompression, which gives lightweight but fast compression.
        ///
        /// Typical speeds of kSnappyCompression on an Intel(R) Core(TM)2 2.4GHz:
        ///    ~200-500MB/s compression
        ///    ~400-800MB/s decompression
        /// Note that these speeds are significantly faster than most
        /// persistent storage speeds, and therefore it is typically never
        /// worth switching to kNoCompression.  Even if the input data is
        /// incompressible, the kSnappyCompression implementation will
        /// efficiently detect that and will switch to uncompressed mode.
        /// </summary>
        public CompressionLevel CompressionLevel {
            set { LevelDbInterop.leveldb_options_set_compression(Handle, (int)value); }
        }

        protected override void FreeUnManagedObjects() {
            if (Handle != default(IntPtr)) LevelDbInterop.leveldb_options_destroy(Handle);
        }

        #region Managed wrappers, must be detached upon successful DB.Create
        public Env InnerEnv { get; set; }

        public Cache InnerCache { get; set; }

        public Comparator InnerComparator { get; set; }
        #endregion
    }
}