# SparseFacetedSearch #

**SparseFacetedSearch** is a faceted searcher based on SimpleFacetedSearch. It uses DocID lists instead on bitmaps.
Efficient memory usage for high cardinality sparsely populated facets.

Suitable for high cardinality, sparsely populated facets.
i.e. There are a large number of facet values and each facet value is hit in a small percentage of documents. Especially if there are also a large number of documents.
SimpleFacetedSearch holds a bitmap for each value representing whether that value is a hit in each document (approx 122KB per 1M documents per facet value).
### Memory requirements ###
fn(Simple):
![equation](http://latex.codecogs.com/gif.latex?\frac{dv}{8})
bytes

Memory increases as the product of documents * values

fn(Sparse):
![equation](http://latex.codecogs.com/gif.latex?4\sum_{n=1..d}^{v}{h}_{v})
bytes

Memory increases in relation to the number of hits only.

Where:

- v = number of values
- h = number of hits
- d = number of documents

So if a document has exactly one value (ie a product category) then fn(Sparse)=d so the memory requirement is 4d bytes.
Compared to fn(Simple) 4d=dv/8 => v=32
So if you have more than 32 values SparseFS is "better".

Generally v=32*hit-ratio where hit-ratio is the average number of hits on a value in each document.
If you have an average of 4 values in a document then the break even is 128 values.