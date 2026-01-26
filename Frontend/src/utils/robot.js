export const generateRobots = (robotCount) => {
    const robots = [];
    for(let i = 0; i < robotCount; i++) {
      robots.push({
        id: `robot-${i}`,
        position: { x: startPos.x, y: startPos.y },
        status: 'ACTIVE',
      });
    }
    return robots;
}

// A: {
//         id: '1',
//         role: 'escort',
//         path: pathA,
//         distanceAlong: 0,
//         status: 'ACTIVE',
//         waitRemainingSec: 0,
//       },