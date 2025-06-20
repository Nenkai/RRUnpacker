using RRUnpacker.Entities;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RRUnpacker.TOC;

public interface ITableOfContents
{
    public void Read();

    public List<RRContainerDescriptor> GetContainers(string fileName);

    public List<RRFileDescriptor> GetFiles(string fileName);
}
