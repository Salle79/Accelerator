import React from 'react';

const Notification = ({ text, top = 'auto', left = 'auto', right = 'auto', display = 'none' }) => (
    <span className='notification__tooltip' style={{ top: `${top}`, left: `${left}`, right: `${right}`, display: `${display}` }}>{text}</span>
);

export default Notification;