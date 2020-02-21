using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;
using System.IO;

namespace Stickman.KeywordService
{
    class KeywordSet : IDisposable
    {
        public KeywordSet()
        {
            
        }

        public KeywordSet(HashSet<string> original)
        {
            if (original == null)
            {
                throw new ArgumentNullException();
            }

            foreach (string item in original)
            {
                m_hashSet.Add(item);
            }
        }

        ~KeywordSet()
        {
            Dispose(false);
        }

        private readonly ReaderWriterLockSlim m_lock = new ReaderWriterLockSlim();
        private readonly HashSet<string> m_hashSet = new HashSet<string>();

        public void Load(string fileName, out ulong user)
        {
            m_lock.EnterWriteLock();
            try
            {
                m_hashSet.Clear();

                using (var br = new BinaryReader(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    user = br.ReadUInt64();

                    int cnt = br.ReadInt32();

                    for (var i = 0; i < cnt; ++i)
                    {
                        m_hashSet.Add(br.ReadString());
                    }

                    br.Close();
                }
            }
            finally
            {
                if (m_lock.IsWriteLockHeld) m_lock.ExitWriteLock();
            }
        }

        public void Save(string fileName, ulong user)
        {
            // 파일 저장은 동시에 발생하면 안되니 Write Lock 사용.
            m_lock.EnterWriteLock();
            try
            {
                using (var bw = new BinaryWriter(new FileStream(fileName, FileMode.Create)))
                {
                    bw.Write(user);

                    bw.Write(m_hashSet.Count);

                    foreach (string keyword in m_hashSet)
                    {
                        bw.Write(keyword);
                    }

                    bw.Close();
                }
            }
            finally
            {
                if (m_lock.IsWriteLockHeld) m_lock.ExitWriteLock();
            }
        }

        public bool Add(string item)
        {
            m_lock.EnterWriteLock();
            try
            {
                return m_hashSet.Add(item);
            }
            finally
            {
                if (m_lock.IsWriteLockHeld) m_lock.ExitWriteLock();
            }
        }

        public void Clear()
        {
            m_lock.EnterWriteLock();
            try
            {
                m_hashSet.Clear();
            }
            finally
            {
                if (m_lock.IsWriteLockHeld) m_lock.ExitWriteLock();
            }
        }

        public bool Contains(string item)
        {
            m_lock.EnterReadLock();
            try
            {
                return m_hashSet.Contains(item);
            }
            finally
            {
                if (m_lock.IsReadLockHeld) m_lock.ExitReadLock();
            }
        }

        public bool Remove(string item)
        {
            m_lock.EnterWriteLock();
            try
            {
                return m_hashSet.Remove(item);
            }
            finally
            {
                if (m_lock.IsWriteLockHeld) m_lock.ExitWriteLock();
            }
        }

        public bool CheckKeywordIn(string text)
        {
            m_lock.EnterReadLock();
            try
            {
                return m_hashSet.Any(keyword => text.Contains(keyword));
            }
            finally
            {
                if (m_lock.IsReadLockHeld) m_lock.ExitReadLock();
            }
        }

        public string[] ToArray()
        {
            m_lock.EnterReadLock();
            try
            {
                return m_hashSet.ToArray();
            }
            finally
            {
                if (m_lock.IsReadLockHeld) m_lock.ExitReadLock();
            }
        }

        public int Count
        {
            get
            {
                m_lock.EnterReadLock();
                try
                {
                    return m_hashSet.Count;
                }
                finally
                {
                    if (m_lock.IsReadLockHeld) m_lock.ExitReadLock();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_lock != null)
                {
                    m_lock.Dispose();
                }
            }
        }
    }
}
