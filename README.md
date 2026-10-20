# Non-Euclidean Traversal

The premise of this project is that you, as a central character,
are traversing over a non-Euclidean 2D manifold. This may be a manifold
embedded in a 3D space, or it may be an arbitrary metric. The central
concept is that the camera is not viewing the character as they traverse
in that embedding space; rather, they are viewing the character within
a projection of that space onto the 2D plane.

There are a few ways to perform this projection; our first attempt will
be to map geodesics radiating away from the character's position over
the non-Euclidean surface and mapping intersecting objects to the 2D
canvas. As the character moves around, they will remain the central
position on the n-E surface and local flat coordinatization will be
performed centred upon the character; new geodesics will then emanate
away from the character to redefine the scene.

So, as the character moves around, the entire 2D world will warp and
transform. You are free to move the camera around and look at the
distorted world, but the distortion does not change if the character
has not moved.

## A simple analytical first manifold: the 2-sphere

As a first step, we will realise a simple traversal system on the 2-sphere.
The system will track the (theta, phi) angular coordinates on a unit sphere;
the coordinate system will stay with the character at the North pole.

We commence with a soccer ball tessellation of the sphere, which is considered
fixed to the surface of the sphere. This allows our 2D canvas to "feel" the
distortion, as we map the vertices in the 3D plane according to the general
idea we outlined above. More precisely,
1. A soccer-ball vertice carries a $(\theta, \phi)$ coordinate.
1. As the character traverses, we track a transformation quaternion which
    describes how the world has rotated relative to the character; however,
    the character is always on the North pole facing south along some
    specific direction, and we transform all the vertices themselves. This
    eases the geodesic mapping to the 2D canvas (see shortly).
1. To achieve the transformation, we track a transformation quaternion of
    the sphere. We then use this transformation to compute the transformed
    $(\theta', \phi')$ coords of the vertices as the character moves around.
1. We now map the 2-sphere to the 2-plane by taking a (theta, phi) point,
    mapping its theta value to a polar radial coord, and its phi value
    to a polar angular coord.

The above mapping is rather simple, but generates an interesting distortion
over the $\theta \in [0, \pi)$ domain as the south pole of our object gets mapped
to the $\theta = \pi$ annular ring.

However, the mapping becomes more exotic as we explore multi-coverings of the
2-sphere. We allow theta to live in $[0, \infty)$, interpreting theta angles
above $\pi$ as continued geodesics emanating away from the start (north pole) point.
That is, as $\theta$ exceeds $\pi$ for the first time, our 2d canvas creates an annular
reproduction of the sphere (albeit with inverted parity); then, as the geodesic
exceeds $2\pi$, another annular representation of the sphere emerges. This continues
add infinitum, if the user zooms out the camera or pans the camera to look at this
exotic infinite world.

Also of interest is that game entities will repeat in each annular realization of
the world. So, if the character moves or launches a projectile or encounters an
enemy, there will exist infinite copies of this gameplay going on at once.

## Some maths

Some notes on the math. The quaternion that we track has components

$$ \cos(\theta/2) + \sin(\theta/2) (u_x \mathbf{i} + u_y \mathbf{j} + u_z \mathbf{k}) $$

where $u_x^2 + u_y^2 + u_z^2 = 1$ form a unit vector storing the axis of rotation
corresponding to the sphere transformation; theta is the angle of rotation; and
$\mathbf{i, j, k}$ are unit quaternion elements squaring to -1 and anti-commuting per
the usual quaternion algebra.

Actually, we consider this an algebraic quaternion only for the algebra's
efficiency in compounding infinitesimal transformations as the player traverses the
world; but in rotation transformations we use the $u_x, u_y, u_z$ and $\theta$ floats
in Rodrigues' formula; i.e, that:

$$ \mathbf{r}' = \mathbf{r} \cos(\theta) + (\mathbf{u} \times \mathbf{r}) \sin(\theta) + \mathbf{u} (\mathbf{u} \cdot \mathbf{v}) [1 - \cos(\theta)] $$
