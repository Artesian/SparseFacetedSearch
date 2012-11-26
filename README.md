# SparseFacetedSearch #

**SparseFacetedSearch** is a faceted searcher based on SimpleFacetedSearch. It uses DocID lists instead of bitmaps.
Efficient memory usage for high cardinality sparsely populated facets.

Suitable for high cardinality, sparsely populated facets.
i.e. There are a large number of facet values and each facet value is hit in a small percentage of documents.
SimpleFacetedSearch holds a bitmap for each value representing whether that value is a hit in each document (approx 122KB per 1M documents per facet value).
### Memory requirements ###
Given the following:

- ![equation](http://latex.codecogs.com/gif.latex?v%20%3D%20%5C%7Bv_1%2Cv_2%2C...%2Cv_n%20%5C%7D), the set of unique facet values
- ![equation](http://latex.codecogs.com/gif.latex?n%20%3D%20%5Cleft%20%7C%20v%20%5Cright%20%7C), the number of unique facet values
- ![equation](http://latex.codecogs.com/gif.latex?h_%7Bv_i%7D%20%3D) number of hits for value ![equation](http://latex.codecogs.com/gif.latex?v_i)
(i.e. the no. of documents the value appears in)
- d = total number of documents

then we have memory requirement given by:

fn(Simple):
![equation](http://latex.codecogs.com/gif.latex?%5Cfrac%7Bdn%7D%7B8%7D)
bytes - hence memory increases as the product of documents * values

fn(Sparse):
![equation](http://latex.codecogs.com/gif.latex?4%5Csum_%7Bi%3D1%7D%5E%7Bn%7Dh_%7Bv_i%7D)
bytes - hence memory increases in relation to the number of hits only.

So, **if every document has exactly one value** (e.g. a product category):
- the sum of all the hit counts is just the total number of documents, and thus the memory requirement is given by fn(Sparse) = 4d bytes.
- the point at which the two methods require equal memory usage is hence when ![equation](http://latex.codecogs.com/gif.latex?4d%20%3D%20%5Cfrac%7Bdn%7D%7B8%7D%20%5CRightarrow%20n%20%3D%2032)
- since fn(Simple) ![equation](http://latex.codecogs.com/gif.latex?%5Cpropto%20n), for ![equation](http://latex.codecogs.com/gif.latex?n%20%3E%2032), SparseFS requires less memory

**More generally**, the point where memory usage is equal for both methods is when n = 32 * hit-ratio, where hit-ratio is the average number of hits per document (across all values).
Hence, if you have an average of 4 values per document then the break even point is 128 values.
Above this number, SparseFS requires less memory than SimpleFS.